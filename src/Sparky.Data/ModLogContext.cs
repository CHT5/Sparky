using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class ModLogContext : DbContext
    {
        private DbSet<ModLogModel> ModLogs { get; set; }

        public ModLogContext()
            => this.Database.EnsureCreated();

        public bool ModLogExists(ulong auditLogId)
            => ModLogs.Any(x => x.AuditLogId == auditLogId);

        public bool ModLogExists(int caseNumber)
            => ModLogs.Any(x => x.CaseNumber == caseNumber);

        public bool TryAddModLog(ModerationAction action, string reason, ulong responsibleUser, ulong targetUser, ulong messageId, out ModLog modlog, ulong? auditLogId = null)
        {
            modlog = null;

            if (auditLogId.HasValue && ModLogExists(auditLogId.Value))
                return false;

            var log = ModLogs.Add(new ModLogModel
            {
                Action = action,
                Reason = reason,
                ResponsibleUserId = responsibleUser,
                AuditLogId = auditLogId,
                CreatedAt = DateTimeOffset.Now,
                MessageId = messageId,
                UserId = targetUser
            });

            SaveChanges();

            modlog = new ModLog(this, log.Entity);

            return true;
        }

        public uint GetLastCaseNumber()
            => this.ModLogs.LastOrDefault()?.CaseNumber ?? 0;

        public ModLog GetModLog(int caseNumber)
        {
            var result = ModLogs.FirstOrDefault(x => x.CaseNumber == caseNumber);

            return new ModLog(this, result);
        }

        protected override void OnModelCreating(ModelBuilder builder)
            => new ModLogModel().ConfigureModel(builder);

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder.UseSqlite("Data Source=Files/Moderation.db");
    }
}