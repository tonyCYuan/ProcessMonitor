using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessMonitor.Models
{
    public class ProcessHistory
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

        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Display(Name = "Path")]
        public string ExecutionPath { get; set; }

        [Display(Name = "Network Connections")]
        public int NetworkConnectionCount { get; set; }

        [Display(Name = "Is Suspicious")]
        public bool IsSuspicious { get; set; }

        [Display(Name = "Alert Triggered")]
        public bool AlertTriggered { get; set; }

        [Display(Name = "Alert Message")]
        public string AlertMessage { get; set; }
    }
}