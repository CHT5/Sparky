using System;
using Sparky.Data.Models;

namespace Sparky.Data
{
    public class PermaModLogEntry : ModLogEntry
    {
        internal PermaModLogEntry(ModLogContext context, PermaModLogModel model) : base(context, model)
        {}

        public PermaModLogEntry Modify(Action<PermaModLogProperties> properties)
        {
            var props = new PermaModLogProperties();

            properties(props);

            base.Modify(x => 
            {
                x.Reason = props.Reason;
                x.ResponsibleModId = props.ResponsibleModId;
                x.ModLogMessageId = props.ModLogMessageId;
            });

            return new PermaModLogEntry(_context, _model as PermaModLogModel);
        }
    }

    public class PermaModLogProperties : ModLogProperties
    {}
}