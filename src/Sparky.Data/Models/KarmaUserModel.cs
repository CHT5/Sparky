using System;
using Microsoft.EntityFrameworkCore;

namespace Sparky.Data.Models
{
    internal class KarmaUserModel : BaseModel
    {
        public ulong UserId { get; set; }

        public uint KarmaCount { get; set; }

        public DateTimeOffset NextKarmaAt { get; set; }

        public DateTimeOffset? LastKarmaGivenAt { get; set; }

        public override void ConfigureModel(ModelBuilder builder)
        {
            var entity = builder.Entity<KarmaUserModel>();

            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId)
                  .ValueGeneratedNever();
            entity.Property(x => x.KarmaCount)
                  .HasDefaultValue(0)
                  .ValueGeneratedOnAdd();
            entity.Property(x => x.NextKarmaAt)
                  .ValueGeneratedNever();
            entity.Property(x => x.LastKarmaGivenAt)
                  .ValueGeneratedNever();
        }
    }
}