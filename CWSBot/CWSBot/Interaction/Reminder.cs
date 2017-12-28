using System;
using CWSBot.Entities;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace CWSBot.Interaction
{
    public class Reminder
    {
        public ulong Id { get; set; }

        public ulong UserId { get; set; }

        public ulong GuildId { get; set; }

        public ulong ChannelId { get; set; }

        public string Content { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset DueTo { get; set; }

        public static ModelBuilder Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Reminder>();
            entity.HasKey(x => new {x.Id, x.UserId, x.ChannelId});

            entity.Property(x => x.Id)
                  .HasValueGenerator<NumericTimestampGenerator>()
                  .ValueGeneratedOnAdd();

            entity.Property(x => x.CreatedAt)
                  .HasValueGenerator<TimestampGenerator>()
                  .ValueGeneratedOnAdd();

            entity.Property(x => x.DueTo)
                  .ValueGeneratedNever();

            entity.Property(x => x.UserId)
                  .ValueGeneratedNever();

            entity.Property(x => x.GuildId)
                  .ValueGeneratedNever();

            entity.Property(x => x.ChannelId)
                  .ValueGeneratedNever();

            entity.Property(x => x.Content)
                  .ValueGeneratedNever();

            return modelBuilder;
        }
    }
}