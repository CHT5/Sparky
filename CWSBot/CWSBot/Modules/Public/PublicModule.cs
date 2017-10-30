using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Diagnostics;
using CWSBot.Interaction;
using Microsoft.Extensions.Configuration;

namespace CWSBot.Modules.Public
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        EasyEmbed embedInfo = new EasyEmbed();

        private CommandService _service;
        private CwsContext _dctx;

        private readonly IConfiguration _config;

        public PublicModule(CommandService service, CwsContext dctx, IConfiguration config)
        {
            _service = service;
            _dctx = dctx;
            _config = config;
        }
        
        //BASIC STATIC COMMANDS
        [Command("help")]
        [Remarks("Shows a list of all available commands per module.")]
        public async Task HelpAsync()
        {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                var description = string.Join("\n", module.Commands.Where(c => c.CheckPreconditionsAsync(Context).Result.IsSuccess).Select(c => $"{prefix}{c.Aliases.First()}"));

                if (!string.IsNullOrWhiteSpace(description))
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
            }

            await dmChannel.SendMessageAsync("", embed: builder.Build());
        }

        [Command("info")]
        public async Task BotInfo()
        {
            var application = await Context.Client.GetApplicationInfoAsync();  /*for lib version*/
            var icon_Info = application.IconUrl;
            var icon_Support = "http://cdn2.hubspot.net/hub/423318/file-2015757038-png/Graphics/Benefits/vControl-Icon-Large_Helpdesk.png";
            var embed_Colour = EasyEmbed.EmbedColour.Red; //SEE THE "EasyEmbed.cs" CLASS FOR THE LIST OF AVAILABLE COLOURS.
            
            using (var process = Process.GetCurrentProcess())
            {
                var upTime = DateTime.Now - process.StartTime;
                var guilds = Context.Client.Guilds;

                embedInfo.CreateFooterEmbed(embed_Colour, $"{application.Name} Status", $"**Owner: ** {application.Owner.Mention}\n" +
                    $"**Discord lib version: **{DiscordConfig.Version}\n" +
                    $"**Guilds: **{guilds.Count}  **Channels: **{guilds.Sum(g => g.Channels.Count)}  **Users: **{guilds.Sum(g => g.Users.Count)}\n" +
                    $"**Uptime: **{upTime.ToString(@"dd\.hh\:mm\:ss")}", icon_Info, $"For issues with, or questions about the bot, please refer to the staff", icon_Support);
            }

            await embedInfo.SendEmbed(Context);
        }

        //I don't like this implementation. See KarmaModule.
        /*[Command("rep")]
        [Remarks("Gives a specified user +1 karma points!")]
        public async Task AddRep(IUser user = null)
        {
            if (user == null)
            {
                await ReplyAsync($"{Context.User.Mention}, you need to specify a user to give karma!");
                return;
            }

            var givingUserStatus = _dctx.Users.SingleOrDefault(x => x.UserId == Context.User.Id);
            var receivingUserStatus = _dctx.Users.SingleOrDefault(x => x.UserId == user.Id);

            TimeSpan diff = DateTime.Now - givingUserStatus.KarmaTime;
            if (diff.Days < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, you need to wait {23 - diff.Hours}h{60 - diff.Minutes}m before you can give karma to someone!");
                return;
            }

            diff = DateTime.Now - receivingUserStatus.KarmaTime;
            if (diff.Days < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, the specified user needs to wait {23 - diff.Hours}h{60 - diff.Minutes}m before he/she can receive karma again.");
                return;
            }

            int warningCounter = receivingUserStatus.WarningCount < 1 ? 0 : -1;

            givingUserStatus.KarmaTime = DateTimeOffset.Now;

            receivingUserStatus.WarningCount += warningCounter;
            receivingUserStatus.Karma++;

            await ReplyAsync($"{Context.User.Mention} just gave {user} +1 karma!");
        }*/
    }
}
 