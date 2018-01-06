using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CWSBot.Interaction
{
    public class MuteContext : DbContext
    {
        public MuteContext() => this.Database.EnsureCreated();

        public DbSet<Mute> Mutes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
            => builder = Mute.Build(builder);

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=mutes.db");
    }
}
