using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CWSBot.Services
{
    public class NameService
    {
        private const string InvalidNickname = "💩";

        private static string[] _attentionSeekingPrefixes = new [] 
                                                            {
                                                                "!"
                                                            };

        public NameService(DiscordSocketClient client)
        {
            client.UserJoined += (user) => {_ = Task.Run(() => CheckUser(user)); return Task.CompletedTask;};
            // I'll figure this out on a later date
            //client.GuildMemberUpdated += (before, user) => {_ = Task.Run(() => CheckUser(user, before)); return Task.CompletedTask;};
        }

        // This will change a users Nickname on Join if
        /// They seek attention (nickname starts with a !)
        /// They have >33% non ascii chars in their name
        // and send a message telling the user about it in mod_logs
        private async Task CheckUser(SocketGuildUser user)
        {
            if (user.IsBot) return;

            if (user.Nickname != null) return;

            if (_attentionSeekingPrefixes.Any(x => user.Username.StartsWith(x)))
                await ChangeNickname(user, true);

            bool IsCharMentionable(char c)
            {
                int numericChar = (int)c;

                return c >= 34 && c <= 126;
            }

            if (user.Username.Sum(x => IsCharMentionable(x) ? 1 : 0) < (user.Username.Length * 0.33))
                await ChangeNickname(user);
        }

        private async Task ChangeNickname(SocketGuildUser user, bool attentionSeeking = false)
        {
            await user.ModifyAsync(x => x.Nickname = InvalidNickname);
            var modChannel = user.Guild.TextChannels.FirstOrDefault(x => x.Name.ToLower() == "mod_logs");
            if (modChannel is null)
            {
                Console.WriteLine($"No mod-log channel found!");
                return;
            }

            string message = $"{user.Mention}\n```\n" +
                             $"Reason: {(attentionSeeking ? "Attention seeking" : "Unmentionable") + " name"}\n" +
                             "Type: Nickname changed\n"+
                             $"Time: {DateTimeOffset.Now}\n```"+
                             "Contact a free moderator for a new nickname";

            await modChannel.SendMessageAsync(message);
        }
    }
}