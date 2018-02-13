using System;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class ModLog
    {
        private readonly ModLogContext _context;

        private readonly ModLogModel _model;

        public uint CaseNumber { get; }

        public ModerationAction Action { get; }

        public DateTimeOffset CreatedAt { get; }

        public ulong? AuditLogId { get; }

        public ulong LogMessageId { get; }

        public ulong? ResponsibleModId { get; }

        public ulong GuildId { get; }

        public string Reason { get; }

        public ulong TargetUserId { get; }

        internal ModLog(ModLogContext context, ModLogModel model)
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
        }

        public ModLog Modify(Action<ModLogProperties> properties)
        {
            var props = new ModLogProperties();
            properties(props);

            if (props.ResponsibleModId.HasValue)
                this._model.ResponsibleUserId = props.ResponsibleModId.Value;

            if (props.Reason != null)
                this._model.Reason = props.Reason;

            this._context.SaveChanges();

            return new ModLog(this._context, this._model);
        }
    }

    public class ModLogProperties
    {
        public ulong? ResponsibleModId { get; set; }

        public string Reason { get; set; }

        internal ModLogProperties()
        {}
    }
}