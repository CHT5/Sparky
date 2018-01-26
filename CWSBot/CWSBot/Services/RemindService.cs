using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CWSBot.Interaction;
using CWSBot.Misc;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Humanizer.Localisation;

namespace CWSBot.Services
{
    public class RemindService
    {
        private const int CheckRate = 1000;

        private const int ReminderLimit = 5;

        private readonly DiscordSocketClient _client;

        private readonly Timer _checkTimer; 

        public RemindService(DiscordSocketClient client)
        {
            this._checkTimer = new Timer((_) => Task.Run(() => CheckRemindersAsync()), null, 0, Timeout.Infinite);
            this._client = client;
        }

        public bool TryAddReminder(IUser user, IGuildChannel channel, string content, DateTimeOffset dueTo)
        {
            using (var context = new RemindContext())
            {
                if (context.Reminders.Count(x => x.UserId == user.Id) >= ReminderLimit) return false;

                context.Add(new Reminder
                {
                    UserId = user.Id,
                    ChannelId = channel.Id,
                    GuildId = channel.GuildId,
                    Content = content,
                    DueTo = dueTo
                });

                context.SaveChanges();

                return true;
            }
        }

        public IEnumerable<Reminder> GetReminders(IUser user, IGuild guild)
        {
            using (var context = new RemindContext())
            {
                var reminders = context.Reminders.Where(x => x.UserId == user.Id && x.GuildId == guild.Id);

                return reminders.OrderByDescending(x => x.DueTo).ToList();
            }
        }

        public bool DeleteReminder(ulong id)
        {
            using (var context = new RemindContext())
            {
                var item = context.Reminders.FirstOrDefault(x => x.Id == id); 

                if (item is null) return false;

                context.Reminders.Remove(item);

                context.SaveChanges();

                return true;
            }
        }

        private Embed GetReminderEmbed(SocketGuildUser user, string content)
        {
            var embed = new EmbedBuilder
            {
                Author = new EmbedAuthorBuilder
                {
                    Name = user.Nickname ?? user.Username,
                    IconUrl = user.GetAvatarUrl(ImageFormat.Auto)
                },
                Description = content,
                Color = user.GetRoleColor()
            };

            return embed.Build();
        }

        private async Task CheckRemindersAsync()
        {
            try
            {
                string GetRemindMessage(IUser user, Reminder reminder)
                    => $"{user.Mention} {(DateTimeOffset.UtcNow - reminder.CreatedAt).Humanize(5, minUnit: TimeUnit.Second, maxUnit: TimeUnit.Year)} ago you asked to be reminded about";

                using (var context = new RemindContext())
                {
                    var dueReminders = context.Reminders
                                              .Where(x => x.DueTo.ToUnixTimeMilliseconds() <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                    if (dueReminders.Count() == 0)
                        return;

                    foreach (var reminder in dueReminders)
                    {
                        var guild = _client.GetGuild(reminder.GuildId);

                        var channel = guild?.GetChannel(reminder.ChannelId);

                        var user = guild?.GetUser(reminder.UserId);

                        if (user is null) continue;

                        if (channel is null)
                        {
                            try
                            {
                                await user.SendMessageAsync(GetRemindMessage(user, reminder), embed: GetReminderEmbed(user, reminder.Content));
                            }
                            catch {}

                            continue;
                        }

                        await (channel as IMessageChannel)?.SendMessageAsync(GetRemindMessage(user, reminder), embed: GetReminderEmbed(user, reminder.Content));
                    }

                    context.Reminders.RemoveRange(dueReminders);

                    context.SaveChanges();
                }
            }
            finally
            {
                this._checkTimer.Change(CheckRate, Timeout.Infinite);
            }
        }
    }
}