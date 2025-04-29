using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProcessMonitor.Models
{
    public class ProcessInfo
    {
        public int Id { get; set; }

        [Display(Name = "Process ID")]
        public int ProcessId { get; set; }

        [Display(Name = "Process Name")]
        public string Name { get; set; }

        [Display(Name = "User")]
        public string UserName { get; set; }

        [Display(Name = "CPU Usage (%)")]
        public double CpuUsage { get; set; }

        [Display(Name = "Memory Usage (MB)")]
        public double MemoryUsageMB { get; set; }

        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Display(Name = "Path")]
        public string ExecutionPath { get; set; }

        [Display(Name = "Command Line")]
        public string CommandLine { get; set; }

        [Display(Name = "Parent Process ID")]
        public int? ParentProcessId { get; set; }

        [Display(Name = "Digitally Signed")]
        public bool IsDigitallySigned { get; set; }

        [Display(Name = "Network Connections")]
        public int NetworkConnectionCount { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public virtual ICollection<ThreadInfo> Threads { get; set; } = new List<ThreadInfo>();
    }
}