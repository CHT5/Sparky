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
using Microsoft.Extensions.Logging;
using Sparky.Objects;
using System;

namespace Sparky.Services
{
    // TODO: Add case reason adding
    public class AuditLogService
    {
        private readonly DiscordClient _client;

        private readonly IConfiguration _config;

        private readonly ModLogContext _modLogContext;

        private readonly ILogger _logger;

        private ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry> _entryCache = new ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry>();

        private ConcurrentDictionary<(ulong, ModerationAction), PendingModerationAction> _actionCache = new ConcurrentDictionary<(ulong, ModerationAction), PendingModerationAction>();

        public AuditLogService(DiscordClient client, IConfiguration config, ILogger<AuditLogService> logger)
        {
            this._client = client;
            this._config = config;
            this._modLogContext = new ModLogContext();
            this._logger = logger;

            this._client.GuildBanAdded += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Ban, args.Member).ConfigureAwait(false));
            this._client.GuildBanRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Unban, args.Member).ConfigureAwait(false));
            this._client.GuildMemberRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Kick, args.Member).ConfigureAwait(false));
            this._client.GuildMemberUpdated += (args) => 
            {
                if (args.RolesBefore.SequenceEqual(args.RolesAfter))
                    return Task.CompletedTask;

                var roles = _config.GetSection("logroles").GetChildren().Select(x => x.Value);

                var differenceAfter = args.RolesAfter.Where(x => !args.RolesBefore.Any(y => y.Id == x.Id));
                var differenceBefore = args.RolesBefore.Where(x => !args.RolesAfter.Any(y => y.Id == x.Id));

                var difference = differenceAfter.Concat(differenceBefore).Distinct();

                if (difference.Any(x => roles.Any(y => y == x.Id.ToString() || y.ToLowerInvariant() == x.Name.ToLowerInvariant())))
                    _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.MemberRoleUpdate, args.Member));

                return Task.CompletedTask;
            }; 
        }

        public async Task<bool> EditReasonAsync(DiscordUser user, string caseNumberString, string reason)
        {
            if (!int.TryParse(caseNumberString, out var caseNumber))
                caseNumber = (int)this._modLogContext.GetLastCaseNumber();

            if (!this._modLogContext.ModLogExists(caseNumber))
                return false;

            var modLog = this._modLogContext.GetModLog(caseNumber);
            modLog = modLog.Modify(x =>
            {
                x.Reason = reason;
                if (!modLog.ResponsibleModId.HasValue)
                    x.ResponsibleModId = user.Id;
            });

            var guild = await this._client.GetGuildAsync(modLog.GuildId);
            var channel = guild?.Channels?.FirstOrDefault(x => x?.Name == _config["channels:mod_log_channel_name"]);
            var message = await channel?.GetMessageAsync(modLog.LogMessageId);
            if (message is null)
                return false;

            DiscordUser responsibleUser = null;
            if (modLog.ResponsibleModId.HasValue)
                responsibleUser = await this._client.GetUserAsync(modLog.ResponsibleModId.Value);

            var target = await this._client.GetUserAsync(modLog.TargetUserId);

            var messageContent = BuildAuditLogMessage(responsibleUser, target, modLog.Action, modLog.CaseNumber, modLog.Reason);

            await message.ModifyAsync(messageContent);

            return true;
        }

        public void AddPendingModerationAction(PendingModerationAction action)
            => this._actionCache.TryAdd((action.Target.Id, action.Action), action);

        private async Task QueryAuditLogsAsync(DiscordGuild guild, AuditLogActionType type, DiscordMember user = null)
        {
            await Task.Delay(500); // wait for the action cache to be updated
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

                            if (this._actionCache.TryGetValue((ban.Target.Id, ModerationAction.Unban), out var resultUnban))
                                await LogModerationType(guild, user, ban, ModerationAction.Unban, resultUnban.Responsible);
                            else
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
                        {
                            if (this._actionCache.TryGetValue((ban.Target.Id, ModerationAction.Softban), out var softBanResult))
                                await LogModerationType(guild, user, ban, ModerationAction.Softban, softBanResult.Responsible);
                            else
                                await LogModerationType(guild, user, ban, ModerationAction.Softban);

                        }
                        else
                        {
                            if (this._actionCache.TryGetValue((ban.Target.Id, ModerationAction.Ban), out var banResult))
                                await LogModerationType(guild, user, ban, ModerationAction.Ban, banResult.Responsible);
                            else
                                await LogModerationType(guild, user, ban, ModerationAction.Ban);
                        }

                        _entryCache.Remove(user, out _);
                        break;

                    case DiscordAuditLogKickEntry kick:
                        if (this._actionCache.TryGetValue((kick.Target.Id, ModerationAction.Kick), out var result))
                                await LogModerationType(guild, user, kick, ModerationAction.Kick, result.Responsible);
                            else
                                await LogModerationType(guild, user, kick, ModerationAction.Kick);
                        break;

                    case DiscordAuditLogMemberUpdateEntry update:
                            if ((update.AddedRoles?.Count ?? 0) != 0)
                            {
                                if (this._actionCache.TryGetValue((update.Target.Id, ModerationAction.SpecialRoleAdded), out var updateResult))
                                    await LogModerationType(guild, user, update, ModerationAction.SpecialRoleAdded, updateResult.Responsible);
                                else
                                    await LogModerationType(guild, user, update, ModerationAction.SpecialRoleAdded);
                            }
                            else if ((update.RemovedRoles?.Count ?? 0) != 0)
                            {
                                if (this._actionCache.TryGetValue((update.Target.Id, ModerationAction.SpecialRoleRemoved), out var updateResult))
                                    await LogModerationType(guild, user, update, ModerationAction.SpecialRoleRemoved, updateResult.Responsible);
                                else
                                    await LogModerationType(guild, user, update, ModerationAction.SpecialRoleRemoved);
                            }
                        break;
                }
            }
        }

        private async Task LogModerationType(DiscordGuild guild, DiscordMember user, DiscordAuditLogEntry entry, ModerationAction type, DiscordMember responsible = null)
        {
            if (this._modLogContext.ModLogExists(entry.Id))
                return;

            this._actionCache.TryRemove((user.Id, type), out _);

            var lastCaseNumber = this._modLogContext.GetLastCaseNumber();
            var responsibleUser = responsible ?? entry.UserResponsible;
            var roleName = string.Empty;

            if (type == ModerationAction.SpecialRoleAdded)
                roleName = (entry as DiscordAuditLogMemberUpdateEntry).AddedRoles.First().Name;
            else if (type == ModerationAction.SpecialRoleRemoved)
                roleName = (entry as DiscordAuditLogMemberUpdateEntry).RemovedRoles.First().Name;

            var logText = BuildAuditLogMessage(responsibleUser, user, type, lastCaseNumber+1, entry.Reason, roleName);
            var logChannel = guild.Channels.FirstOrDefault(x => x.Name == _config["channels:mod_log_channel_name"]);
            var message = await logChannel.SendMessageAsync(logText);
            this._modLogContext.TryAddModLog(type, entry.Reason, user.Id, message.Id, guild.Id, out _, (entry.UserResponsible.Id == this._client.CurrentUser.Id ? responsible?.Id : entry.UserResponsible.Id), entry.Id);
        }

        private string BuildAuditLogMessage(DiscordUser responsibleUser, DiscordUser target, ModerationAction type, uint caseNumber, string reason = null, string roleName = null)
        {
            var responsibleText = string.Empty;
            if (responsibleUser.Id != this._client.CurrentUser.Id)
                responsibleText = $"**Responsible staff member**: {responsibleUser.Username}#{responsibleUser.Discriminator}";
            else
                responsibleText = $"**Responsible staff member**: _Responsible moderator, please type `{_config["prefix"]}sign {caseNumber}`_";

            var logEntry = $"**{type.Humanize()}{(!string.IsNullOrEmpty(roleName) ? $": {roleName}" : string.Empty)}** | Case {caseNumber}\n" +
                           $"**User**: {target.Username}#{target.Discriminator} ({target.Id}) ({target.Mention})\n" +
                           $"**Reason**: {reason ?? $"_Responsible moderator, please type `{_config["prefix"]}reason {caseNumber} <reason>`_"}\n" +
                           responsibleText;
            return logEntry;
        }
    }
}