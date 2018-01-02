using System;
using System.Linq;
using System.Threading.Tasks;
using CWSBot.Misc;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Localisation;

namespace CWSBot.Modules
{
    public class ReminderModule : ModuleBase<SocketCommandContext>
    {
        private readonly RemindService _remindService;

        public ReminderModule(RemindService remindService)
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

            public ReminderGroup(RemindService remindService)
                => this._remindService = remindService;

            [Command("show")]
            public Task ShowRemindersAsync()
            {
                var reminders = this._remindService.GetReminders(Context.User, Context.Guild);

                if (reminders.Count() == 0)
                    return ReplyAsync("You currently have no active reminders!");

                var embed = new EmbedBuilder
                {
                    Title = "Active Reminders",
                    Color = Context.User.GetRoleColor()
                };

                for (var i = 1; i <= reminders.Count(); i++)
                    embed.AddField($"Reminder {i}", reminders.ElementAtOrDefault(i-1)?.ToString() ?? "Error");

                return ReplyAsync("", embed: embed.Build());
            }
        }
    }
}