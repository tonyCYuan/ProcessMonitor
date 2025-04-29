using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcessMonitor.Data;
using ProcessMonitor.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;

namespace ProcessMonitor.Services
{
    public class ProcessMonitorService : IProcessMonitorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProcessMonitorService> _logger;

        public ProcessMonitorService(ApplicationDbContext context, ILogger<ProcessMonitorService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProcessInfo>> GetAllProcessesAsync()
        {
            try
            {
                return await _context.ProcessInfos
                    .Include(p => p.Threads)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all processes");
                return new List<ProcessInfo>();
            }
        }

        public async Task<ProcessInfo> GetProcessByIdAsync(int processId)
        {
            try
            {
                return await _context.ProcessInfos
                    .Include(p => p.Threads)
                    .FirstOrDefaultAsync(p => p.ProcessId == processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving process with ID {processId}");
                return null;
            }
        }

        public async Task<List<ThreadInfo>> GetThreadsByProcessIdAsync(int processId)
        {
            try
            {
                var process = await _context.ProcessInfos
                    .Include(p => p.Threads)
                    .FirstOrDefaultAsync(p => p.ProcessId == processId);

                return process?.Threads.ToList() ?? new List<ThreadInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving threads for process ID {processId}");
                return new List<ThreadInfo>();
            }
        }

        public async Task<bool> TerminateProcessAsync(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                
                // Remove from database
                var dbProcess = await _context.ProcessInfos
                    .FirstOrDefaultAsync(p => p.ProcessId == processId);
                    
                if (dbProcess != null)
                {
                    _context.ProcessInfos.Remove(dbProcess);
                    await _context.SaveChangesAsync();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error terminating process with ID {processId}");
                return false;
            }
        }

        public async Task<List<ProcessInfo>> GetSuspiciousProcessesAsync()
        {
            try
            {
                // Get all alert rules
                var rules = await _context.AlertRules
                    .Where(r => r.IsEnabled)
                    .ToListAsync();

                // Get all processes
                var processes = await _context.ProcessInfos.ToListAsync();
                
                var suspiciousProcesses = new List<ProcessInfo>();
                
                foreach (var process in processes)
                {
                    foreach (var rule in rules)
                    {
                        bool isSuspicious = false;
                        
                        // Check name pattern
                        if (!string.IsNullOrEmpty(rule.ProcessNamePattern) && 
                            process.Name.Contains(rule.ProcessNamePattern, StringComparison.OrdinalIgnoreCase))
                        {
                            isSuspicious = true;
                        }
                        
                        // Check CPU threshold
                        if (rule.CpuThreshold.HasValue && process.CpuUsage > rule.CpuThreshold.Value)
                        {
                            isSuspicious = true;
                        }
                        
                        // Check memory threshold
                        if (rule.MemoryThreshold.HasValue && process.MemoryUsageMB > rule.MemoryThreshold.Value)
                        {
                            isSuspicious = true;
                        }
                        
                        // Check network connections
                        if (rule.NetworkConnectionsThreshold.HasValue && 
                            process.NetworkConnectionCount > rule.NetworkConnectionsThreshold.Value)
                        {
                            isSuspicious = true;
                        }
                        
                        // Check digital signature
                        if (rule.MustBeDigitallySigned.HasValue && 
                            rule.MustBeDigitallySigned.Value && !process.IsDigitallySigned)
                        {
                            isSuspicious = true;
                        }
                        
                        if (isSuspicious)
                        {
                            suspiciousProcesses.Add(process);
                            break; // No need to check other rules
                        }
                    }
                }
                
                return suspiciousProcesses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving suspicious processes");
                return new List<ProcessInfo>();
            }
        }

        public async Task ScanAndUpdateProcessesAsync()
        {
            try
            {
                _logger.LogInformation("Starting process scan...");
                
                // Get all running processes on the system
                var runningProcesses = Process.GetProcesses();
                var existingProcesses = await _context.ProcessInfos.ToListAsync();
                var updatedProcessIds = new List<int>();
                
                foreach (var process in runningProcesses)
                {
                    try
                    {
                        // Check if process already exists in DB
                        var existingProcess = existingProcesses.FirstOrDefault(p => p.ProcessId == process.Id);
                        
                        if (existingProcess != null)
                        {
                            // Update existing process info
                            UpdateProcessInfo(existingProcess, process);
                            updatedProcessIds.Add(process.Id);
                        }
                        else
                        {
                            // Create new process info
                            var newProcess = CreateProcessInfo(process);
                            _context.ProcessInfos.Add(newProcess);
                            updatedProcessIds.Add(process.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error processing information for process ID {process.Id}");
                    }
                }
                
                // Remove processes that are no longer running
                var processesToRemove = existingProcesses.Where(p => !updatedProcessIds.Contains(p.ProcessId)).ToList();
                if (processesToRemove.Any())
                {
                    _context.ProcessInfos.RemoveRange(processesToRemove);
                }
                
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Process scan completed. Updated {updatedProcessIds.Count} processes, removed {processesToRemove.Count} processes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during process scan");
            }
        }

        public async Task<List<ProcessHistory>> GetProcessHistoryAsync(int processId, int limit = 100)
        {
            try
            {
                return await _context.ProcessHistory
                    .Where(p => p.ProcessId == processId)
                    .OrderByDescending(p => p.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving history for process ID {processId}");
                return new List<ProcessHistory>();
            }
        }

        private void UpdateProcessInfo(ProcessInfo existingProcess, Process process)
        {
            try
            {
                // Update basic info
                existingProcess.Name = process.ProcessName;
                existingProcess.CpuUsage = GetProcessCpuUsage(process);
                existingProcess.MemoryUsageMB = process.WorkingSet64 / 1024.0 / 1024.0;
                existingProcess.NetworkConnectionCount = GetNetworkConnectionCount(process.Id);
                existingProcess.LastUpdated = DateTime.UtcNow;
                
                // Update thread information
                UpdateThreads(existingProcess, process);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error updating information for process ID {process.Id}");
            }
        }

        private ProcessInfo CreateProcessInfo(Process process)
        {
            var processInfo = new ProcessInfo
            {
                ProcessId = process.Id,
                Name = process.ProcessName,
                UserName = "Unknown",
                CpuUsage = GetProcessCpuUsage(process),
                MemoryUsageMB = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                Threads = new List<ThreadInfo>(),
                LastUpdated = DateTime.UtcNow
            };
            
            try
            {
                processInfo.StartTime = process.StartTime.ToUniversalTime();
            }
            catch
            {
                processInfo.StartTime = DateTime.UtcNow;
            }
            
            try
            {
                processInfo.UserName = GetProcessOwner(process.Id);
            }
            catch
            {
                processInfo.UserName = "Unknown";
            }
            
            // Handle each extended property individually to avoid one failure affecting others
            try
            {
                processInfo.ExecutionPath = process.MainModule?.FileName ?? "Unknown";
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                processInfo.ExecutionPath = "Access Denied";
                _logger.LogDebug($"Cannot access main module for process ID {process.Id}: {ex.Message}");
            }
            
            try
            {
                processInfo.CommandLine = GetCommandLine(process.Id);
            }
            catch (Exception ex)
            {
                processInfo.CommandLine = "Unknown";
                _logger.LogDebug($"Cannot retrieve command line for process ID {process.Id}: {ex.Message}");
            }
            
            try
            {
                processInfo.ParentProcessId = GetParentProcessId(process.Id);
            }
            catch (Exception ex)
            {
                processInfo.ParentProcessId = null;
                _logger.LogDebug($"Cannot retrieve parent process ID for process ID {process.Id}: {ex.Message}");
            }
            
            try
            {
                processInfo.IsDigitallySigned = IsDigitallySigned(process.MainModule?.FileName);
            }
            catch (Exception ex)
            {
                processInfo.IsDigitallySigned = false;
                _logger.LogDebug($"Cannot verify digital signature for process ID {process.Id}: {ex.Message}");
            }
            
            try
            {
                processInfo.NetworkConnectionCount = GetNetworkConnectionCount(process.Id);
            }
            catch (Exception ex)
            {
                processInfo.NetworkConnectionCount = 0;
                _logger.LogDebug($"Cannot count network connections for process ID {process.Id}: {ex.Message}");
            }
            
            // Create thread information
            UpdateThreads(processInfo, process);
            
            return processInfo;
        }

        private void UpdateThreads(ProcessInfo processInfo, Process process)
        {
            try
            {
                var existingThreadIds = processInfo.Threads.Select(t => t.ThreadId).ToList();
                var currentThreads = process.Threads.Cast<ProcessThread>().ToList();
                var currentThreadIds = currentThreads.Select(t => t.Id).ToList();
                
                // Remove threads that no longer exist
                var threadsToRemove = processInfo.Threads.Where(t => !currentThreadIds.Contains(t.ThreadId)).ToList();
                foreach (var thread in threadsToRemove)
                {
                    processInfo.Threads.Remove(thread);
                }
                
                // Update or add threads
                foreach (ProcessThread processThread in currentThreads)
                {
                    var existingThread = processInfo.Threads.FirstOrDefault(t => t.ThreadId == processThread.Id);
                    
                    if (existingThread != null)
                    {
                        // Update existing thread
                        existingThread.State = processThread.ThreadState.ToString();
                        existingThread.Priority = processThread.BasePriority;
                        existingThread.LastUpdated = DateTime.UtcNow;
                    }
                    else
                    {
                        // Create new thread
                        var newThread = new ThreadInfo
                        {
                            ThreadId = processThread.Id,
                            State = processThread.ThreadState.ToString(),
                            Priority = processThread.BasePriority,
                            StartTime = DateTime.UtcNow, // Approximation as actual thread start time is not easily available
                            CpuUtilization = 0, // Will need a more complex mechanism to track this accurately
                            LastUpdated = DateTime.UtcNow,
                            ProcessInfoId = processInfo.ProcessId
                        };
                        
                        processInfo.Threads.Add(newThread);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error updating thread information for process ID {process.Id}");
            }
        }

        private double GetProcessCpuUsage(Process process)
        {
            // Note: Accurate CPU usage calculation requires multiple measurements over time
            // This is a simplistic approach to get an approximate value
            try
            {
                process.Refresh();
                return Math.Round(process.TotalProcessorTime.TotalMilliseconds / 
                    (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds * 100, 2);
            }
            catch
            {
                return 0;
            }
        }

        private string GetProcessOwner(int processId)
        {
            string owner = "Unknown";
            
            try
            {
                var connectionOptions = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate
                };
                var scope = new ManagementScope(@"\\.\root\cimv2", connectionOptions);
                scope.Connect();
                
                var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = {processId}");
                
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(5);
                    
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject process in collection)
                        {
                            try
                            {
                                string[] argList = new string[] { string.Empty, string.Empty };
                                int returnValue = Convert.ToInt32(process.InvokeMethod("GetOwner", argList));
                                
                                if (returnValue == 0)
                                {
                                    owner = argList[1] + "\\" + argList[0]; // Domain\User
                                }
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                }
            }
            catch (ManagementException mex)
            {
                _logger.LogError(mex, $"WMI error when retrieving owner for process ID {processId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving owner for process ID {processId}");
            }
            
            return owner;
        }

        private string GetCommandLine(int processId)
        {
            string commandLine = "Unknown";
            
            try
            {
                var connectionOptions = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate
                };
                var scope = new ManagementScope(@"\\.\root\cimv2", connectionOptions);
                scope.Connect();
                
                var query = new ObjectQuery($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
                
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(5);
                    
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject process in collection)
                        {
                            try
                            {
                                var cmdLine = process["CommandLine"];
                                commandLine = cmdLine != null ? cmdLine.ToString() : "Unknown";
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                }
            }
            catch (ManagementException mex)
            {
                _logger.LogError(mex, $"WMI error when retrieving command line for process ID {processId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving command line for process ID {processId}");
            }
            
            return commandLine;
        }

        private int? GetParentProcessId(int processId)
        {
            try
            {
                var connectionOptions = new ConnectionOptions
                {
                    Impersonation = ImpersonationLevel.Impersonate
                };
                var scope = new ManagementScope(@"\\.\root\cimv2", connectionOptions);
                scope.Connect();
                
                var query = new ObjectQuery($"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {processId}");
                
                using (var searcher = new ManagementObjectSearcher(scope, query))
                {
                    searcher.Options.Timeout = TimeSpan.FromSeconds(5);
                    
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject process in collection)
                        {
                            try
                            {
                                var parentId = process["ParentProcessId"];
                                if (parentId != null)
                                {
                                    return Convert.ToInt32(parentId);
                                }
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                    }
                }
            }
            catch (ManagementException mex)
            {
                _logger.LogError(mex, $"WMI error when retrieving parent process ID for process ID {processId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving parent process ID for process ID {processId}");
            }
            
            return null;
        }

        private bool IsDigitallySigned(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return false;
            }
            
            try
            {
                X509Certificate cert = X509Certificate.CreateFromSignedFile(filePath);
                return cert != null;
            }
            catch
            {
                return false;
            }
        }

        private int GetNetworkConnectionCount(int processId)
        {
            try
            {
                // Use netstat command to count connections
                // This is a simplistic approach - production code should use proper APIs
                using (Process netstat = new Process())
                {
                    netstat.StartInfo.FileName = "netstat";
                    netstat.StartInfo.Arguments = "-ano";
                    netstat.StartInfo.UseShellExecute = false;
                    netstat.StartInfo.RedirectStandardOutput = true;
                    netstat.Start();
                    
                    string output = netstat.StandardOutput.ReadToEnd();
                    netstat.WaitForExit();
                    
                    return output.Split('\n')
                        .Count(line => line.Contains(processId.ToString()) && 
                                      (line.Contains("TCP") || line.Contains("UDP")));
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}