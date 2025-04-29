using System;
using System.ComponentModel.DataAnnotations;

namespace ProcessMonitor.Models
{
    public class AlertRule
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Rule Name")]
        public string Name { get; set; }

        [Display(Name = "Process Name Pattern")]
        public string ProcessNamePattern { get; set; }

        [Display(Name = "CPU Threshold (%)")]
        public double? CpuThreshold { get; set; }

        [Display(Name = "Memory Threshold (MB)")]
        public double? MemoryThreshold { get; set; }

        [Display(Name = "Network Connections Threshold")]
        public int? NetworkConnectionsThreshold { get; set; }

        [Display(Name = "Must Be Digitally Signed")]
        public bool? MustBeDigitallySigned { get; set; }

        [Display(Name = "Alert On Startup")]
        public bool AlertOnStartup { get; set; }

        [Display(Name = "Email Notification")]
        public bool SendEmailNotification { get; set; }

        [EmailAddress]
        [Display(Name = "Email Recipients")]
        public string EmailRecipients { get; set; }

        [Display(Name = "Is Enabled")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Modified Date")]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    }
}