using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CWSBot.Services
{
    public class NameService
    {
        private const string InvalidNickname = "Unmentionable";

        private static string[] _attentionSeekingPrefixes = new [] 
                                {
                                    "!"
                                };

        public NameService(DiscordSocketClient client)
        {
            client.UserJoined += (user) => {_ = Task.Run(() => CheckUserAsync(user)); return Task.CompletedTask;};
            // I'll figure this out on a later date
            //client.GuildMemberUpdated += (before, user) => {_ = Task.Run(() => CheckUser(user, before)); return Task.CompletedTask;};
        }

        // This will change a users Nickname on Join if
        /// They seek attention (Name starts with a !)
        /// They have >33% non ascii chars in their name
        // and send a message telling the user about it in mod_logs
        private async Task CheckUserAsync(SocketGuildUser user)
        {
            if (user.IsBot) return;

            if (user.Nickname != null) return;

            if (_attentionSeekingPrefixes.Any(x => user.Username.StartsWith(x)))
                await ChangeNicknameAsync(user, true);

            int CalculateScore(string input)
            {
                bool IsCharMentionable(char c)
                    => c >= 34 && c <= 126;

                var enumerator = StringInfo.GetTextElementEnumerator(input);

                int score = 0;

                while (enumerator.MoveNext())
                {
                    var current = (string)enumerator.Current;

                    if (!char.TryParse(current, out char c))
                        score += 0;
                    else
                        score += IsCharMentionable(c) ? 1 : 0;
                }

                return score;
            }

            if (CalculateScore(user.Username) < Math.Min(3, new StringInfo(user.Username).LengthInTextElements * 0.33))
                await ChangeNicknameAsync(user);
        }

        private async Task ChangeNicknameAsync(SocketGuildUser user, bool attentionSeeking = false)
        {
            await user.ModifyAsync(x => x.Nickname = InvalidNickname);
            var modChannel = user.Guild.TextChannels.FirstOrDefault(x => x.Name.ToLower() == "mod_logs");
            if (modChannel is null)
            {
                Console.WriteLine($"No mod_log channel found!");
                return;
            }

            string message = $"{user.Mention}\n```\n" +
                             $"Reason: {(attentionSeeking ? "Attention seeking" : "Unmentionable") + " name"}\n" +
                             "Type: Nickname changed\n"+
                             $"Time: {DateTimeOffset.Now}\n```"+
                             "Contact an available moderator for a new nickname";

            await modChannel.SendMessageAsync(message);
        }
    }
}
