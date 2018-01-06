using System;
using System.Text;
using CWSBot.Entities;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace CWSBot.Interaction
{
    public class Mute
    {
        private const string DateTimeFormat = @"dd\.MM\.yyyy hh\:mm\:ss";

        public ulong Id { get; set; }

        public ulong ActorId { get; set; }

        public ulong MutedId { get; set; }

        public ulong GuildId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset DueAt { get; set; }

        public static ModelBuilder Build(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Mute>();

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasValueGenerator<NumericTimestampGenerator>()
                .ValueGeneratedOnAdd();

            entity.Property(x => x.CreatedAt)
                .HasValueGenerator<TimestampGenerator>()
                .ValueGeneratedOnAdd();

            entity.Property(x => x.ActorId)
                .ValueGeneratedNever();

            entity.Property(x => x.MutedId)
                .ValueGeneratedNever();

            entity.Property(x => x.DueAt)
                .ValueGeneratedNever();

            entity.Property(x => x.GuildId)
                .ValueGeneratedNever();

            return modelBuilder;
        }
    }
}
