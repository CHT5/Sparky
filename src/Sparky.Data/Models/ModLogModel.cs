using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sparky.Data.Models
{
    internal class ModLogModel : BaseModel
    {
        public uint CaseNumber { get; set; }

        public ulong GuildId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong? AuditLogId { get; set; }

        public ModerationAction Action { get; set; }

        public ulong UserId { get; set; }

        public ulong? MessageId { get; set; }

        public ulong? ResponsibleUserId { get; set; }

        public ulong? RoleAdded { get; set; }

        public string Reason { get; set; }

        public string TargetUsername { get; set; }

        public string TargetDiscriminator { get; set; }

        public override void ConfigureModel(ModelBuilder builder)
        {
            var entity = builder.Entity<ModLogModel>();
            entity.HasKey(x => x.CaseNumber);
            /*entity.Property(x => x.CaseNumber)
                  .ValueGeneratedOnAdd();*/
            entity.Property(x => x.AuditLogId)
                  .ValueGeneratedNever();
            entity.Property(x => x.MessageId)
                  .ValueGeneratedNever();
            entity.Property(x => x.ResponsibleUserId)
                  .ValueGeneratedNever();
            builder.Entity<PermaModLogModel>().HasBaseType<ModLogModel>();
            builder.Entity<TimedModLogModel>().HasBaseType<ModLogModel>();
        }
    }
}