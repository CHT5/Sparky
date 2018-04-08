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

        private DispatchService _dispatchService;

        private ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry> _entryCache = new ConcurrentDictionary<DiscordMember, DiscordAuditLogEntry>();

        private ConcurrentDictionary<(ulong, ModerationAction), PendingModerationAction> _actionCache = new ConcurrentDictionary<(ulong, ModerationAction), PendingModerationAction>();

        public AuditLogService(DiscordClient client, IConfiguration config, ILogger<AuditLogService> logger, ModLogContext modLogContext)
        {
            this._client = client;
            this._config = config;
            this._modLogContext = modLogContext;
            this._logger = logger;

            this._client.GuildBanAdded += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Ban, args.Member).ConfigureAwait(false));
            this._client.GuildBanRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Unban, args.Member).ConfigureAwait(false));
            this._client.GuildMemberRemoved += (args) => _ = Task.Run(() => QueryAuditLogsAsync(args.Guild, AuditLogActionType.Kick, args.Member).ConfigureAwait(false));
            this._client.GuildMemberUpdated += (args) => 
            {
                if (args.RolesBefore.SequenceEqual(args.RolesAfter))
                    return Task.CompletedTask; // roles didn't change, no action to take

                var roles = _config.GetSection("logroles").GetChildren().Select(x => x.Value);

                var differenceAfter = args.RolesAfter.Where(x => !args.RolesBefore.Any(y => y.Id == x.Id)); // Get all distinct new roles
                var differenceBefore = args.RolesBefore.Where(x => !args.RolesAfter.Any(y => y.Id == x.Id)); // Get all distinct old roles

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
            var message = await channel?.GetMessageAsync(modLog.LogMessageId ?? 0);
            if (message is null)
                return false;

            DiscordUser responsibleUser = null;
            if (modLog.ResponsibleModId.HasValue)
                responsibleUser = await this._client.GetUserAsync(modLog.ResponsibleModId.Value);

            var target = await this._client.GetUserAsync(modLog.TargetUserId);

            var messageContent = await modLog.GenerateMessageAsync(this._client, this._config);

            await message.ModifyAsync(messageContent);

            return true;
        }

        public void AddPendingModerationAction(PendingModerationAction action)
            => this._actionCache.TryAdd((action.Target.Id, action.Action), action);

        private async Task QueryAuditLogsAsync(DiscordGuild guild, AuditLogActionType type, DiscordMember user = null)
        {
            await Task.Delay(500); // wait for the action cache to be updated
            var logs = await guild.GetAuditLogsAsync(1, null, type).ConfigureAwait(false);

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

                        int waitingTime = 30000;

                        if (this._actionCache.TryGetValue((user.Id, ModerationAction.Ban), out _) ||
                            this._actionCache.TryGetValue((user.Id, ModerationAction.TemporaryBan), out _))
                            waitingTime = 1000;

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
                            await Task.Delay(waitingTime, cancelToken.Token);
                        }
                        catch
                        {}

                        if (unbanned)
                            await LogModerationType(guild, user, ban, ModerationAction.Softban);
                        else
                            await LogModerationType(guild, user, ban, this._actionCache.TryGetValue((user.Id, ModerationAction.TemporaryBan), out _) ? ModerationAction.TemporaryBan : ModerationAction.Ban);
                        
                        _entryCache.Remove(user, out _);
                        break;

                    case DiscordAuditLogKickEntry kick:
                            await LogModerationType(guild, user, kick, ModerationAction.Kick);
                        break;

                    case DiscordAuditLogMemberUpdateEntry update:
                            if ((update.AddedRoles?.Count ?? 0) != 0) // :)
                                await LogModerationType(guild, user, update, this._actionCache.TryGetValue((user.Id, ModerationAction.TemporarySpecialRoleAdded), out _) ? ModerationAction.TemporarySpecialRoleAdded : ModerationAction.SpecialRoleAdded);
                            else if ((update.RemovedRoles?.Count ?? 0) != 0)
                                await LogModerationType(guild, user, update, ModerationAction.SpecialRoleRemoved);
                        break;
                }
            }
        }

        public void AddDispatchService(DispatchService service)
            => this._dispatchService = this._dispatchService ?? service; // Prevent from overwriting

        private async Task LogModerationType(DiscordGuild guild, DiscordMember user, DiscordAuditLogEntry entry, ModerationAction type)
        {
            if (this._modLogContext.ModLogExists(entry.Id))
                return;

            this._actionCache.TryRemove((user.Id, type), out var modAction);

            var lastCaseNumber = this._modLogContext.GetLastCaseNumber();
            var responsibleUser = modAction?.Responsible ?? entry.UserResponsible;
            var roleName = string.Empty;
            ulong? roleId = null;

            if (type == ModerationAction.SpecialRoleAdded || type == ModerationAction.TemporarySpecialRoleAdded)
            {
                var role = (entry as DiscordAuditLogMemberUpdateEntry).AddedRoles.First();
                roleName = role.Name;
                roleId = role.Id;
            }
            else if (type == ModerationAction.SpecialRoleRemoved)
                roleName = (entry as DiscordAuditLogMemberUpdateEntry).RemovedRoles.First().Name;

            ModLogEntry modLogEntry = null;

            if (type.IsTemporary())
            {
                modLogEntry = this._modLogContext.AddModLog<TimedModLogCreationProperties>((timed) => 
                {
                    timed.Action = type;
                    timed.Reason = entry.Reason;
                    timed.UserId = user.Id;
                    timed.GuildId = guild.Id;
                    timed.EndsAt = modAction.EndsAt.Value;
                    timed.AuditLogId = entry.Id;
                    timed.RoleAdded = roleId;

                    if (entry.UserResponsible.Id == this._client.CurrentUser.Id)
                    {
                        timed.ResponsibleUserId = modAction.Responsible.Id;
                        timed.TargetDiscriminator = modAction.Target.Discriminator;
                        timed.TargetUsername = modAction.Target.Username;
                    }
                    else
                    {
                        timed.ResponsibleUserId = entry.UserResponsible.Id;
                        timed.TargetDiscriminator = user.Discriminator;
                        timed.TargetUsername = user.Username;
                    }
                });

                this._dispatchService.TryEnqueueModAction(modLogEntry as TimedModLogEntry);
            }
            else
            {
                modLogEntry = this._modLogContext.AddModLog<ModLogCreationProperties>((timed) => 
                {
                    timed.Action = type;
                    timed.Reason = entry.Reason;
                    timed.UserId = user.Id;
                    timed.GuildId = guild.Id;
                    timed.AuditLogId = entry.Id;
                    timed.RoleAdded = roleId;

                    if (entry.UserResponsible.Id == this._client.CurrentUser.Id)
                    {
                        timed.ResponsibleUserId = modAction.Responsible.Id;
                        timed.TargetDiscriminator = modAction.Target.Discriminator;
                        timed.TargetUsername = modAction.Target.Username;
                    }
                    else
                    {
                        timed.ResponsibleUserId = entry.UserResponsible.Id;
                        timed.TargetDiscriminator = user.Discriminator;
                        timed.TargetUsername = user.Username;
                    }
                });
            }

            await modLogEntry.SendLogAsync(guild, this._config, this._client);
        }
    }
}