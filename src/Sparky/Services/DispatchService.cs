using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sparky.Data;
using Sparky.Objects;

namespace Sparky.Services
{
    public class DispatchService
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<Type, TimedDispatchTask> _currentDispatchTasks;

        private readonly DiscordClient _client;

        private readonly AuditLogService _auditLogService;

        private bool _initalQueryDone = false;

        public DispatchService(DiscordClient client, IConfiguration configuration, ILogger<DispatchService> logger, AuditLogService auditLogService)
        {
            this._client = client;
            this._configuration = configuration;
            this._logger = logger;
            this._auditLogService = auditLogService;
            this._currentDispatchTasks = new ConcurrentDictionary<Type, TimedDispatchTask>();
            client.Ready += _ =>
            {
                if (!this._initalQueryDone)
                {
                    QueryDatabases();
                    this._initalQueryDone = true;
                }
                return Task.CompletedTask;
            };
        }

        public void QueryDatabases()
        {
            using (var modContext = new ModLogContext())
            {
                if (modContext.TryGetNextModLog(out var timedBan, ModerationAction.TemporaryBan))
                    TryToQueueDispatch(typeof(TimedBanDispatchTask), timedBan);

                if (modContext.TryGetNextModLog(out var timedSpecialRoleAdded, ModerationAction.TemporarySpecialRoleAdded))
                    TryToQueueDispatch(typeof(TimedSpecialRoleDispatchTask), timedSpecialRoleAdded);
            }
        }

        public bool TryEnqueueModAction(TimedModLogEntry entry)
        {
            switch (entry.Action)
            {
                case ModerationAction.TemporaryBan:
                    return TryToQueueDispatch(typeof(TimedBanDispatchTask), entry);
                
                case ModerationAction.TemporarySpecialRoleAdded:
                    return TryToQueueDispatch(typeof(TimedSpecialRoleDispatchTask), entry);

                default:
                    throw new NotSupportedException($":thinking:"); // Shouldn't happen tbh
            }
        }

        private bool TryToQueueDispatch(Type type, TimedModLogEntry modLogEntry)
        {
            var timedTask = GetDispatchTask(modLogEntry);
            timedTask.Dispatched += OnDispatched;
            return this._currentDispatchTasks.AddOrUpdate(type, timedTask, (_, oldTask) => 
            {
                if (timedTask.DueTo == oldTask.DueTo)
                    return timedTask;

                if (oldTask.DueTo == default(DateTimeOffset))
                    return timedTask;

                // Old task will finish earlier, nothing to do
                if (oldTask.DueTo > timedTask.DueTo)
                {
                    timedTask.Dispatched -= OnDispatched;
                    timedTask.Cancel();
                    return oldTask;
                }

                timedTask.Start();
                return timedTask;
            }).DueTo == timedTask.DueTo;
        }

        private TimedDispatchTask GetDispatchTask(TimedModLogEntry modLogEntry)
        {
            async Task<bool> TryWaitUntilFinishedAsync(TimeSpan time, CancellationToken token)
            {
                if (time < TimeSpan.FromSeconds(0))
                    return true;

                try
                {
                    await Task.Delay(time, token);
                }
                catch
                {
                    return false;
                }

                return true;
            }

            switch(modLogEntry.Action)
            {
                case ModerationAction.TemporaryBan:
                    return new TimedBanDispatchTask(async cancelToken => 
                    {
                        if (!await TryWaitUntilFinishedAsync(modLogEntry.EndsAt - DateTimeOffset.Now, cancelToken))
                            return BanDispatchResult.FromCancelled(modLogEntry);

                        var guild = await this._client.GetGuildAsync(modLogEntry.GuildId);
                        var user = await this._client.GetUserAsync(modLogEntry.TargetUserId);
                        this._auditLogService.AddPendingModerationAction(new PendingModerationAction(ModerationAction.Unban, this._client.CurrentUser, user));
                        try
                        {
                            await user.UnbanAsync(guild, $"Automated unban after {(modLogEntry.EndsAt - modLogEntry.CreatedAt).Humanize()}");
                        }
                        catch {}
                        return BanDispatchResult.FromSuccess(user, modLogEntry);
                    }, modLogEntry.EndsAt);

                case ModerationAction.TemporarySpecialRoleAdded:
                    return new TimedSpecialRoleDispatchTask(async cancelToken => 
                    {
                        if (!await TryWaitUntilFinishedAsync(modLogEntry.EndsAt - DateTimeOffset.Now, cancelToken))
                            return SpecialRoleDispatchResult.FromCancelled(modLogEntry);

                        var guild = await this._client.GetGuildAsync(modLogEntry.GuildId);
                        var member = await guild.GetMemberAsync(modLogEntry.TargetUserId);
                        var role = guild.GetRole(modLogEntry.RoleAdded.Value);

                        if (member == null && role == null) // Member left in the time and role was deleted
                            return SpecialRoleDispatchResult.FromFailed(modLogEntry: modLogEntry);
                        else if (member == null && role != null)
                            return SpecialRoleDispatchResult.FromFailed(member: member, modLogEntry: modLogEntry); // Role was deleted
                        else if (member != null && role == null)
                            return SpecialRoleDispatchResult.FromFailed(role: role, modLogEntry: modLogEntry); // Member left

                        this._auditLogService.AddPendingModerationAction(new PendingModerationAction(ModerationAction.SpecialRoleRemoved, this._client.CurrentUser, member));
                        await member.RevokeRoleAsync(role);

                        return SpecialRoleDispatchResult.FromSuccess(role, member, modLogEntry);
                    }, modLogEntry.EndsAt);
                default:
                    throw new ArgumentException("Unhandled DispatchTask type");
            }
        }

        private Task OnDispatched(DispatchResult result)
        {
            switch (result)
            {
                case BanDispatchResult banDispatch:
                    (banDispatch.ModLogEntry as TimedModLogEntry).Modify(x => x.Completed = true);

                break;

                case SpecialRoleDispatchResult roleDispatch:
                    (roleDispatch.ModLogEntry as TimedModLogEntry).Modify(x => x.Completed = true);
                break;
            }

            QueryDatabases();

            return Task.CompletedTask;
        }
    }
}