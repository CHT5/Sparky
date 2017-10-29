using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace CWSBot.Services
{
    public class AnnounceService
    {
        private const string FormatLang = "ini";

        private readonly DiscordSocketClient _client;

        private readonly IConfiguration _config;

        private bool _announcing;

        public AnnounceService(DiscordSocketClient client, IConfiguration config)
        {
            this._client = client;
            this._config = config;
            this._announcing = true;
            this.SubscribeEvents();
        }

        public bool TryStoppingAnnouncing()
        {

        }

        public bool TryResumingAnnouncing()
        {

        }

        public async Task AccountJoined(SocketGuildUser user)
        {
            var textChannels = user.Guild.TextChannels;
            var roles = user.Guild.Roles;
            var welcomeChannelPublic = textChannels.FirstOrDefault(x => x.Name == this._config["public_welcome_channel_name"]);
            var welcomeChannelMod = textChannels.FirstOrDefault(x => x.Name == this._config["mod_welcome_channel_name"]);
            var learningRole = roles.FirstOrDefault(x => x.Name == this._config["learning_role_name"]);
            var botsRole = roles.FirstOrDefault(x => x.Name == this._config["bot_role_name"]);

            var userWelcomeText = new StringBuilder(user.IsBot ? "The Bot " : string.Empty);
            userWelcomeText.Append($"[{user.Username}] has joined the server!");

            var roleAddedText = new StringBuilder($"[{(user.IsBot ? botsRole.Name : learningRole.Name)}]");
            roleAddedText.Append($" has been assigned to [{user.Username}]!");
            await user.AddRoleAsync(user.IsBot ? botsRole : learningRole);

            await welcomeChannelMod.SendMessageAsync(Format.Code(userWelcomeText.ToString(), FormatLang));
            await welcomeChannelPublic.SendMessageAsync(Format.Code(userWelcomeText.ToString(), FormatLang));
            await welcomeChannelMod.SendMessageAsync(Format.Code(roleAddedText.ToString(), FormatLang));
        }

        public async Task AccountLeft(SocketGuildUser user)
        {
            var goodbyeChannelMod = user.Guild.TextChannels.FirstOrDefault(x => x.Name == this._config["mod_goodbye_channel_name"]);

            await goodbyeChannelMod.SendMessageAsync(Format.Code($"[{user.Username}] left the server!", FormatLang));
        }

        private void SubscribeEvents()
        {

        }

        private void UnsubscribeEvents()
        {

        }
    }
}