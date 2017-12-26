using CWSBot.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CWSBot.Interaction
{
    public class LogContext : DbContext
    {
        public LogContext()
        {
            this.Database.EnsureCreated();
        }

        public DbSet<ModLog> Modlogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite("Data Source=mod_logs.db");
        }
    }
}
