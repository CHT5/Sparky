using System;
using DSharpPlus.Entities;
using Sparky.Data;

namespace Sparky.Objects
{
    public class SpecialRoleDispatchResult : DispatchResult
    {
        public DiscordRole Role { get; }

        public DiscordMember Member { get; }

        public TimedModLogEntry ModLogEntry { get; set; }

        public SpecialRoleDispatchResult(DiscordRole role, DiscordMember member, DispatchType type, TimedModLogEntry modLogEntry, DateTimeOffset? dispatchedAt = null) : base(type, dispatchedAt)
        {
            this.Role = role;
            this.Member = member;
            this.ModLogEntry = modLogEntry;
        }

        public static SpecialRoleDispatchResult FromSuccess(DiscordRole role, DiscordMember member, TimedModLogEntry modLogEntry)
            => new SpecialRoleDispatchResult(role, member, DispatchType.Successful, modLogEntry, DateTimeOffset.Now);

        public static SpecialRoleDispatchResult FromFailed(TimedModLogEntry modLogEntry, DiscordRole role = null, DiscordMember member = null)
            => new SpecialRoleDispatchResult(role, member, DispatchType.Failed, modLogEntry);

        public static SpecialRoleDispatchResult FromCancelled(TimedModLogEntry modLogEntry)
            => new SpecialRoleDispatchResult(null, null, DispatchType.Cancelled, modLogEntry);
    }
}