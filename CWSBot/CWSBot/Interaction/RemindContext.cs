using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CWSBot.Interaction
{
    public class RemindContext : DbContext
    {
        public DbSet<Reminder> Reminders { get; set; }

        public RemindContext()
            => this.Database.EnsureCreated();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder = Reminder.Build(modelBuilder);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=reminders.db");
    }
}