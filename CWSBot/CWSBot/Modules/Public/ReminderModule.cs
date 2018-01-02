using System;
using System.Linq;
using System.Threading.Tasks;
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

        [Command("remindme")]
        [RequireContext(ContextType.Guild)]
        public Task SetReminderAsync(TimeSpan dueTo, [Remainder] string content)
        {
            if (!this._remindService.TryAddReminder(Context.User, Context.Channel as IGuildChannel, content, DateTimeOffset.UtcNow.Add(dueTo)))
                return ReplyAsync("You have already reached the maximum allowed reminders!");
            else
                return ReplyAsync($"I will remind you in {dueTo.Humanize(5, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)} about: {Format.Sanitize(content)}!");
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

            [Command("show")]
            public Task ShowRemindersAsync()
            {
                var interactive = new InteractiveReminderMessage(Context.User, Context.Channel as ITextChannel);

                return this._interactiveService.SendInteractiveMessageAsync(interactive);
            }
        }
    }
}