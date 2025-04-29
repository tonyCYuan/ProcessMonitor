using Microsoft.EntityFrameworkCore;
using ProcessMonitor.Models;

namespace ProcessMonitor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProcessInfo> ProcessInfos { get; set; }
        public DbSet<ThreadInfo> ThreadInfos { get; set; }
        public DbSet<AlertRule> AlertRules { get; set; }
        public DbSet<ProcessHistory> ProcessHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<ThreadInfo>()
                .HasOne(t => t.Process)
                .WithMany(p => p.Threads)
                .HasForeignKey(t => t.ProcessInfoId);

            // Configure any other model constraints or indexes
            modelBuilder.Entity<ProcessInfo>()
                .HasIndex(p => p.ProcessId);

            modelBuilder.Entity<ThreadInfo>()
                .HasIndex(t => t.ThreadId);

            modelBuilder.Entity<AlertRule>()
                .HasIndex(a => a.Name)
                .IsUnique();

            modelBuilder.Entity<ProcessHistory>()
                .HasIndex(p => p.Timestamp);
        }
    }
}