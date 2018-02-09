using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class KarmaContext : DbContext
    {
        private DbSet<KarmaUserModel> Users { get; set; }

        public KarmaContext()
            => Database.EnsureCreated();

        public KarmaInformation GetKarmaInformation(ulong userId)
        {
            var karmaData = GetOrCreateKarmaData(userId);

            return new KarmaInformation(this, karmaData);
        }

        private KarmaUserModel GetOrCreateKarmaData(ulong userId)
        {
            if (!Users.Any(x => x.UserId == userId))
            {
                this.Users.Add(new KarmaUserModel
                {
                    UserId = userId,
                    NextKarmaAt = DateTimeOffset.Now
                });

                SaveChanges();
            }

            return Users.First(x => x.UserId == userId);
        }

        protected override void OnModelCreating(ModelBuilder builder)
            => new KarmaUserModel().ConfigureModel(builder);

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder.UseSqlite("Data Source=Files/Karma.db");
    }
}