using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CWSBot.Config;
using System.Diagnostics;

namespace CWSBot.Modules.Public
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        EasyEmbed embedInfo = new EasyEmbed();
        private CommandService _service;
        public PublicModule(CommandService service)
        {
            _service = service;
        }
        
        //BASIC STATIC COMMANDS
        [Command("help")]
        [Remarks("Shows a list of all available commands per module.")]
        public async Task HelpAsync()
        {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

            string prefix = BotConfig.Load().Prefix;
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };

            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases.First()}\n";
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await dmChannel.SendMessageAsync("", false, builder.Build());
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
                var time = DateTime.Now - process.StartTime;


                embedInfo.createFooterEmbed(embed_Colour, $"{application.Name} Status", $"**Owner: ** {application.Owner.Mention}\n" +
                    $"**Discord lib version: **{DiscordConfig.Version}\n" +
                    $"**Guilds: **{(Context.Client as DiscordSocketClient).Guilds.Count.ToString()}  **Channels: **{(Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Channels.Count).ToString()}  **Users: **{(Context.Client as DiscordSocketClient).Guilds.Sum(g => g.Users.Count).ToString()}\n" +
                    $"**Uptime: **{time.ToString(@"dd\.hh\:mm\:ss")}", icon_Info, $"For issues with, or questions about the bot, please refer to the staff", icon_Support);
            }
            await embedInfo.sendEmbed(Context);
        }

        //USER-SPECIFIED COMMANDS
        [Command("rep")]
        [Remarks("Gives a specified user +1 karma points!")]
        public async Task AddRep(IUser user = null)
        {
            if (user == null)
            {
                await ReplyAsync($"{Context.User.Mention}, you need to specify a user to give karma!");
                return;
            }
            var givingUserStatus = Database.GetUserStatus(Context.User);
            var receivingUserSTatus = Database.GetUserStatus(user);
            TimeSpan diff = DateTime.Now - givingUserStatus.FirstOrDefault().karmaParticipation;

            if (diff.Days < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, you need to wait {23 - diff.Hours}h{60 - diff.Minutes}m before you can give karma to someone!");
                return;
            }
            diff = DateTime.Now - receivingUserSTatus.FirstOrDefault().karmaParticipation;
            if (diff.Days < 1)
            {
                await ReplyAsync($"{Context.User.Mention}, the specified user needs to wait {23 - diff.Hours}h{60 - diff.Minutes}m before he/she can receive karma again.");
                return;
            }
            int warningCounter = -1;
            if(receivingUserSTatus.FirstOrDefault().warningCount < 1)
            {
                warningCounter = 0;
            }
            Database.RepDateChange(Context.User);
            Database.RepUser(user, warningCounter);
            await ReplyAsync($"{Context.User.Mention} just gave {user} +1 karma!");
        }
    }
}
 