using System;
using DSharpPlus.Entities;
using Sparky.Data;

namespace Sparky.Objects
{
    public class PendingModerationAction
    {
        public ModerationAction Action { get; }

        public DiscordUser Responsible { get; }

        public DiscordUser Target { get; }

        public DateTimeOffset? EndsAt { get; }

        public PendingModerationAction(ModerationAction action, DiscordUser responsible, DiscordUser target, DateTimeOffset? endsAt = null)
        {
            this.Action = action;
            this.Responsible = responsible;
            this.Target = target;
            this.EndsAt = endsAt;
        }
    }
}