using System;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class ModLogEntry
    {
        private protected readonly ModLogContext _context;

        private protected readonly ModLogModel _model;

        public uint CaseNumber { get; }

        public DateTimeOffset CreatedAt { get; }

        public ModerationAction Action { get; }

        public string TargetUsername { get; }

        public string TargetDiscriminator { get; }

        public ulong? AuditLogId { get; }

        public ulong? LogMessageId { get; }

        public ulong? ResponsibleModId { get; }

        public ulong GuildId { get; }

        public string Reason { get; }

        public ulong TargetUserId { get; }

        public ulong? RoleAdded { get; }

        internal ModLogEntry(ModLogContext context, ModLogModel model)
        {
            this._context = context;
            this._model = model;
            this.CaseNumber = model.CaseNumber;
            this.Action = model.Action;
            this.CreatedAt = model.CreatedAt;
            this.AuditLogId = model.AuditLogId;
            this.LogMessageId = model.MessageId;
            this.ResponsibleModId = model.ResponsibleUserId;
            this.Reason = model.Reason;
            this.GuildId = model.GuildId;
            this.TargetUserId = model.UserId;
            this.RoleAdded = model.RoleAdded;
            this.TargetDiscriminator = model.TargetDiscriminator;
            this.TargetUsername = model.TargetUsername;
        }

        public virtual ModLogEntry Modify(Action<ModLogProperties> properties)
        {
            var props = new ModLogProperties();
            properties(props);

            if (props.ResponsibleModId.HasValue)
                this._model.ResponsibleUserId = props.ResponsibleModId.Value;

            if (props.Reason != null)
                this._model.Reason = props.Reason;

            if (props.ModLogMessageId != null)
                this._model.MessageId = props.ModLogMessageId;

            this._context.SaveChanges();

            return new ModLogEntry(this._context, this._model);
        }
    }

    public class ModLogProperties
    {
        public ulong? ResponsibleModId { get; set; }

        public string Reason { get; set; }

        public ulong? ModLogMessageId { get; set; }

        internal ModLogProperties()
        {}
    }
}