using ProcessMonitor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessMonitor.Services
{
    public interface IProcessMonitorService
    {
        Task<List<ProcessInfo>> GetAllProcessesAsync();
        Task<ProcessInfo> GetProcessByIdAsync(int processId);
        Task<List<ThreadInfo>> GetThreadsByProcessIdAsync(int processId);
        Task<bool> TerminateProcessAsync(int processId);
        Task<List<ProcessInfo>> GetSuspiciousProcessesAsync();
        Task ScanAndUpdateProcessesAsync();
        Task<List<ProcessHistory>> GetProcessHistoryAsync(int processId, int limit = 100);
    }
}