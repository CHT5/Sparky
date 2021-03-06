using System;
using System.Linq;
using System.Threading.Tasks;
using CWSBot.Entities;
using CWSBot.Entities.Interactive;
using CWSBot.Misc;
using CWSBot.Services;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Localisation;

namespace CWSBot.Modules
{
    public class ReminderModule : ModuleBase<SocketCommandContext>
    {
        private readonly RemindService _remindService;

        public ReminderModule(RemindService remindService, InteractiveService interactiveService)
            => this._remindService = remindService;

        [Command("remindme", RunMode = RunMode.Async)]
        [RequireContext(ContextType.Guild)]
        public Task SetReminderAsync(TimeSpan dueTo, [Remainder, LimitTo(512)] string content)
        {
            if (!this._remindService.TryAddReminder(Context.User, Context.Channel as IGuildChannel, content, DateTimeOffset.UtcNow.Add(dueTo)))
                return ReplyAsync("You have already reached the maximum allowed reminders!");
            else
            {
                EmbedBuilder embed = new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        IconUrl = Context.User.GetAvatarUrl(ImageFormat.Auto),
                        Name = (Context.User as IGuildUser)?.Nickname ?? Context.User.Username
                    },
                    Description = content,
                    Color = Context.User.GetRoleColor()
                };

                return ReplyAsync($"I will remind you in {dueTo.Humanize(5, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)} about: ", embed: embed.Build());
            }
        }

        [Group("reminder")]
        [Alias("reminders")]
        [RequireContext(ContextType.Guild)]
        public class ReminderGroup : ModuleBase<SocketCommandContext>
        {
            private readonly RemindService _remindService;

            private readonly InteractiveService _interactiveService;

            public ReminderGroup(RemindService remindService, InteractiveService interactiveService)
            {
                this._remindService = remindService;
                this._interactiveService = interactiveService;
            }

            [Command("show", RunMode = RunMode.Async)]
            public Task ShowRemindersAsync()
            {
                var interactive = new InteractiveReminderMessage(Context.Channel as ITextChannel, Context.User);

                return this._interactiveService.SendInteractiveMessageAsync(interactive);
            }

            [Command("mod show", RunMode = RunMode.Async)]
            [RequireUserPermission(GuildPermission.BanMembers)] // People with Ban perms should be trustworthy enough to
                                                                // be able to delete reminders responsibly
            public Task ModShowRemindersAsync([Remainder] IGuildUser user)
            {
                var interactive = new InteractiveReminderMessage(Context.Channel as ITextChannel, Context.User, user);

                return this._interactiveService.SendInteractiveMessageAsync(interactive);
            }
        }
    }
}