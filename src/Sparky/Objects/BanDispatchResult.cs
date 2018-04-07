using System;
using DSharpPlus.Entities;
using Sparky.Data;

namespace Sparky.Objects
{
    public class BanDispatchResult : DispatchResult
    {
        public DiscordUser User { get; }

        public TimedModLogEntry ModLogEntry { get; }

        public BanDispatchResult(DiscordUser user, DispatchType type, TimedModLogEntry modLogEntry, DateTimeOffset? dispatchedAt = null) : base(type, dispatchedAt)
        {
            this.User = user;
            this.ModLogEntry = modLogEntry;
        }

        public static BanDispatchResult FromSuccess(DiscordUser user, TimedModLogEntry modLogEntry)
            => new BanDispatchResult(user, DispatchType.Successful, modLogEntry, DateTimeOffset.Now);

        public static BanDispatchResult FromFailed(TimedModLogEntry modLogEntry, DiscordUser user = null)
            => new BanDispatchResult(user, DispatchType.Failed, modLogEntry);

        public static BanDispatchResult FromCancelled(TimedModLogEntry modLogEntry)
            => new BanDispatchResult(null, DispatchType.Cancelled, modLogEntry);
    }
}