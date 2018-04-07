using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class ModLogContext : DbContext
    {
        internal DbSet<ModLogModel> ModLogs { get; set; }

        public ModLogContext()
            => this.Database.EnsureCreated();

        public bool ModLogExists(ulong auditLogId)
            => ModLogs.Any(x => x.AuditLogId == auditLogId);

        public bool ModLogExists(int caseNumber)
            => ModLogs.Any(x => x.CaseNumber == caseNumber);

        public ModLogEntry AddModLog<T>(Action<T> properties)
            where T: ModLogCreationProperties
        {
            var props = Activator.CreateInstance(typeof(T)) as T;

            properties(props);

            if (props.AuditLogId.HasValue && ModLogExists(props.AuditLogId.Value))
                return null;

            ModLogModel rawLog;

            switch (props)
            {
                case TimedModLogCreationProperties timedProps:
                    rawLog = new TimedModLogModel
                    {
                        Action = timedProps.Action,
                        Reason = timedProps.Reason,
                        ResponsibleUserId = timedProps.ResponsibleUserId,
                        AuditLogId = timedProps.AuditLogId,
                        CreatedAt = DateTimeOffset.Now,
                        UserId = timedProps.UserId,
                        GuildId = timedProps.GuildId,
                        RoleAdded = timedProps.RoleAdded,
                        EndsAt = timedProps.EndsAt,
                        TargetDiscriminator = timedProps.TargetDiscriminator,
                        TargetUsername = timedProps.TargetUsername,

                    };
                break;

                default:
                    rawLog = new PermaModLogModel
                    {
                        Action = props.Action,
                        Reason = props.Reason,
                        ResponsibleUserId = props.ResponsibleUserId,
                        AuditLogId = props.AuditLogId,
                        CreatedAt = DateTimeOffset.Now,
                        UserId = props.UserId,
                        GuildId = props.GuildId,
                        RoleAdded = props.RoleAdded,
                        TargetDiscriminator = props.TargetDiscriminator,
                        TargetUsername = props.TargetUsername,
                    };
                break;
            }

            var resLog = ModLogs.Add(rawLog);

            SaveChanges();

            return GetModLog((int)resLog.Entity.CaseNumber);
        }

        public uint GetLastCaseNumber()
            => this.ModLogs.LastOrDefault()?.CaseNumber ?? 0;

        public ModLogEntry GetModLog(int caseNumber)
        {
            var result = ModLogs.FirstOrDefault(x => x.CaseNumber == caseNumber);

            switch (result)
            {
                case TimedModLogModel timed:
                    return new TimedModLogEntry(new ModLogContext(), timed);

                case PermaModLogModel perma:
                    return new PermaModLogEntry(new ModLogContext(), perma);

                default:
                    return new ModLogEntry(new ModLogContext(), result);
            }
        }

        public bool TryGetNextModLog(out TimedModLogEntry entry, ModerationAction? action = null)
        {
            var results = ModLogs.Where(x => x is TimedModLogModel).OrderByDescending(x => (x as TimedModLogModel).EndsAt);
            var result = results.FirstOrDefault(x => x.Action == action && !(x as TimedModLogModel).Completed);
            if (result != null)
                entry = new TimedModLogEntry(new ModLogContext(), result as TimedModLogModel);
            else
                entry = null;

            return result != null;
        }

        protected override void OnModelCreating(ModelBuilder builder)
            => new ModLogModel().ConfigureModel(builder);

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
            => builder.UseSqlite("Data Source=Files/Moderation.db");
    }
}