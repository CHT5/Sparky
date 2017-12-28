using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

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
                return ReplyAsync("Reminder successfully set!");
        }
    }
}