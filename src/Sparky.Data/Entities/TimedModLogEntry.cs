using System;
using System.Linq;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class TimedModLogEntry : ModLogEntry
    {
        public DateTimeOffset EndsAt { get; }

        internal TimedModLogEntry(ModLogContext context, TimedModLogModel model) : base(context, model)
        {
            this.EndsAt = model.EndsAt;
        }

        public TimedModLogEntry Modify(Action<TimedModLogProperties> properties)
        {
            var props = new TimedModLogProperties();
            properties(props);

            try // :(
            {
                if (props.Completed != null)
                    (this._context.ModLogs.FirstOrDefault(x => x.CaseNumber == this.CaseNumber) as TimedModLogModel).Completed = props.Completed.Value;
            }
            catch {}

            this._context.SaveChanges();

            base.Modify(x =>
            {
                x.ModLogMessageId = props.ModLogMessageId;
                x.Reason = props.Reason;
                x.ResponsibleModId = props.ResponsibleModId;
            });

            return new TimedModLogEntry(this._context, this._model as TimedModLogModel);
        }
    }

    public class TimedModLogProperties : ModLogProperties
    {
        public bool? Completed { get; set; }
    }
}