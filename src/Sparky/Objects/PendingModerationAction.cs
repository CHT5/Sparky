using DSharpPlus.Entities;
using Sparky.Data;

namespace Sparky.Objects
{
    public struct PendingModerationAction
    {
        public ModerationAction Action { get; }

        public DiscordMember Responsible { get; }

        public DiscordMember Target { get; }

        public PendingModerationAction(ModerationAction action, DiscordMember responsible, DiscordMember target)
        {
            this.Action = action;
            this.Responsible = responsible;
            this.Target = target;
        }
    }
}