using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Sparky.Attributes;
using Sparky.Data;
using Sparky.Objects;
using Sparky.Services;

namespace Sparky.Modules
{
    public class ModerationModule : SparkyBaseModule
    {
        private readonly AuditLogService _auditLogService;

        private readonly DispatchService _dispatchService;

        public ModerationModule(AuditLogService auditLogService, DispatchService dispatchService)
        {
            this._auditLogService = auditLogService;
            this._dispatchService = dispatchService;
        }

        [Command("reason")]
        [RequireConfiguredRole(RoleCheckMode.Any, "roles:admin", "roles:moderator")]
        public async Task EditReasonAsync(CommandContext context, string caseNumber, [RemainingText] string reason)
        {
            if(await this._auditLogService.EditReasonAsync(context.User, caseNumber, reason))
                await context.Message.CreateReactionAsync(AcceptedEmoji);
            else
                await context.Message.CreateReactionAsync(DeniedEmoji);
        }

        [Command("kick")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task KickAsync(CommandContext ctx, DiscordMember member, [RemainingText] string reason = null)
        {
            try
            {
                await member.RemoveAsync(reason);
                var modAction = new PendingModerationAction(ModerationAction.Kick, ctx.User as DiscordMember, member);
                this._auditLogService.AddPendingModerationAction(modAction);
                await ctx.Message.CreateReactionAsync(AcceptedEmoji);
            }
            catch
            {
                await ctx.Message.CreateReactionAsync(DeniedEmoji);
            }
        }

        [Command("softban")]
        [RequireUserPermissions(Permissions.BanMembers)]
        public Task SoftbanAsync(CommandContext context, DiscordMember member, [RemainingText] string reason = null)
            => SoftbanAsync(context, member, 7, reason);

        [Command("softban")]
        [Priority(1)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task SoftbanAsync(CommandContext context, DiscordMember member, int pruneDays = 7, [RemainingText] string reason = null)
        {
            try
            {
                await member.BanAsync(pruneDays, reason);
                var modAction = new PendingModerationAction(ModerationAction.Softban, context.User as DiscordMember, member);
                this._auditLogService.AddPendingModerationAction(modAction);
                await Task.Delay(500);
                await member.UnbanAsync();
                await context.Message.CreateReactionAsync(AcceptedEmoji);
            }
            catch
            {
                await context.Message.CreateReactionAsync(DeniedEmoji);
            }
        }

        [Command("ban")]
        public Task BanAsync(CommandContext context, DiscordMember member, [RemainingText] string reason = null)
            => BanAsync(context, member, 7, reason); 

        [Command("ban")]
        [Priority(1)]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task BanAsync(CommandContext context, DiscordMember member, int pruneDays = 7, [RemainingText] string reason = null)
        {
            try
            {
                await member.BanAsync(pruneDays, reason);
                var modAction = new PendingModerationAction(ModerationAction.Ban, context.User as DiscordMember, member);
                this._auditLogService.AddPendingModerationAction(modAction);
                await context.Message.CreateReactionAsync(AcceptedEmoji);
            }
            catch
            {
                await context.Message.CreateReactionAsync(DeniedEmoji);
            }
        }

        [Command("tempban")]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task TempBanAsync(CommandContext context, DiscordMember member, int time, [RemainingText] string reason = null)
        {
            try
            {
                await member.BanAsync(7, $"{reason} (Temporary)");
                var modAction = new PendingModerationAction(ModerationAction.TemporaryBan, context.User as DiscordMember, member, DateTimeOffset.Now.Add(TimeSpan.FromMinutes(time)));
                this._auditLogService.AddPendingModerationAction(modAction);
                await context.Message.CreateReactionAsync(AcceptedEmoji);
            }
            catch
            {
                await context.Message.CreateReactionAsync(DeniedEmoji);
            }
        }
    }
}