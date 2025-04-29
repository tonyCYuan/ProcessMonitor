using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessMonitor.Models
{
    public class ThreadInfo
    {
        public int Id { get; set; }

        [Display(Name = "Thread ID")]
        public int ThreadId { get; set; }

        [Display(Name = "Thread State")]
        public string State { get; set; }

        [Display(Name = "CPU Utilization (%)")]
        public double CpuUtilization { get; set; }

        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Display(Name = "Priority")]
        public int Priority { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        // Foreign key
        public int ProcessInfoId { get; set; }
        
        // Navigation property
        public virtual ProcessInfo Process { get; set; }
    }
}