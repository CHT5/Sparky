using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Sparky.Data.Models
{
    internal class ModLogModel : BaseModel
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public uint CaseNumber { get; set; }

        public ModerationAction Action { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public ulong? AuditLogId { get; set; }

        public ulong UserId { get; set; }

        public ulong MessageId { get; set; }

        public ulong ResponsibleUserId { get; set; }

        public string Reason { get; set; }

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
        }
    }
}