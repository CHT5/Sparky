using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Sparky.Data;
using Humanizer;
using System.Collections.Concurrent;

namespace Sparky.Services
{
    // TODO: Add case reason adding
    public class AuditLogService
    {
        private readonly DiscordClient _client;

        private readonly IConfiguration _config;

        private readonly ModLogContext _modLogContext;

        private ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry> _entryCache = new ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry>();

        public AuditLogService(DiscordClient client, IConfiguration config)
        {
            this._client = client;
            this._config = config;
            this._modLogContext = new ModLogContext();

            this._client.GuildBanAdded += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Ban, args.Member).ConfigureAwait(false));
            this._client.GuildBanRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Unban, args.Member).ConfigureAwait(false));
            this._client.GuildMemberRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Kick, args.Member).ConfigureAwait(false));
        }

        private async Task QueryAuditLogsAsync(DiscordGuild guild, AuditLogActionType type, DiscordMember user = null)
        {
            var logs = await guild.GetAuditLogsAsync(1, null, type);

            foreach (var log in logs)
            {
                switch(log)
                {
                    case DiscordAuditLogBanEntry ban:
                        if (type == AuditLogActionType.Unban)
                        {
                            if (_entryCache.TryGetValue(user, out DiscordAuditLogEntry entry))
                                return; // If this happens it means that the user was softbanned, no need to log the unban

                            await LogModerationType(guild, user, ban, ModerationAction.Unban);
                            return;
                        }

                        _entryCache.TryAdd(user, ban);

                        bool unbanned = false;
                        var cancelToken = new CancellationTokenSource();
                        this._client.GuildBanRemoved += (args) => 
                        {
                            if (args.Member.Id == user.Id)
                                unbanned = true;

                            cancelToken.Cancel();

                            return Task.CompletedTask;
                        };
                        try
                        {
                            await Task.Delay(30*1000, cancelToken.Token);
                        }
                        catch
                        {}

                        if (unbanned)
                            await LogModerationType(guild, user, ban, ModerationAction.Softban);
                        else
                            await LogModerationType(guild, user, ban, ModerationAction.Ban);

                        _entryCache.Remove(user, out _);
                        break;

                    case DiscordAuditLogKickEntry kick:
                        await LogModerationType(guild, user, kick, ModerationAction.Kick);
                        break;
                }
            }
        }

        private async Task LogModerationType(DiscordGuild guild, DiscordMember user, DiscordAuditLogEntry entry, ModerationAction type)
        {
            if (this._modLogContext.ModLogExists(entry.Id))
                return;

            var lastCaseNumber = this._modLogContext.GetLastCaseNumber();

            var logChannel = guild.Channels.FirstOrDefault(x => x.Name == "mod_logs");
            var logEntry = $"**{type.Humanize()}** | Case {lastCaseNumber+1}\n" +
                           $"**User**: {user.Username}#{user.Discriminator} ({user.Id}) ({user.Mention})\n" +
                           $"**Reason**: {entry.Reason ?? "No reason provided"}\n" +
                           $"**Responsible staff member**: {entry.UserResponsible.Username}#{entry.UserResponsible.Discriminator} ({entry.UserResponsible.Mention})";

            var message = await logChannel.SendMessageAsync(logEntry);

            this._modLogContext.TryAddModLog(type, entry.Reason, entry.UserResponsible.Id, user.Id, message.Id, out _, entry.Id);
        }
    }
}