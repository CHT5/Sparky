using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CWSBot.Config;

namespace CWSBot.Modules.Public
{
    public class PublicModule : ModuleBase<SocketCommandContext>
    {

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

        [Command("status")]
        [Alias("s")]
        [Remarks("Shows the specified user's status")]
        public async Task Status(IUser user = null)
        {
            if (user == null)
            {
                user = Context.User;
            }
            await ReplyAsync("stuff");
        }

        //SELF-SPECIFIED NON-STATIC COMMANDS
        [Command("addrole")] //Command Name
        [Remarks("Adds a specific role of choice")] //Summary for your command. it will not add anything.
        public async Task AddRole(SocketRole roleChoice)
        {
            //SET User and Guild
            var guild = Context.Client.GetGuild(351284764352839690);
            var user = guild.GetUser(Context.User.Id);

            

            //SPECIFIC GAMING CHANNELS
            if (roleChoice.Name == "league")
            {
                var roleName = "Game_League";
                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "league-of-legends") as SocketTextChannel;

                var userRoles = user.Roles.FirstOrDefault(has => has.Name.ToUpper() == roleName.ToUpper());
                if(userRoles == null)
                {
                    await user.AddRoleAsync(roleChoice);
                    await channel.SendMessageAsync($"{user.Mention}, welcome to the {channel.Name} channel!");
                }
                else
                {
                    await ReplyAsync($"{user.Mention}, you already have the specified role!");
                }
                return;
            }
            if (roleChoice.Name == "ow")
            {
                var roleName = "Game_Overwatch";
                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "overwatch") as SocketTextChannel;

                var userRoles = user.Roles.FirstOrDefault(has => has.Name.ToUpper() == roleName.ToUpper());
                if (userRoles == null)
                {
                    await user.AddRoleAsync(roleChoice);
                    await channel.SendMessageAsync($"{user.Mention}, welcome to the {channel.Name} channel!");
                }
                else
                {
                    await ReplyAsync($"{user.Mention}, you already have the specified role!");
                }
                return;
            }

            //SPECIFIC PROGRAMMING CHANNELS
            if (roleChoice.Name == "csharp")
            {
                var roleName = "Coding_CSharp";
                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "csharp") as SocketTextChannel;

                var userRoles = user.Roles.FirstOrDefault(has => has.Name.ToUpper() == roleName.ToUpper());
                if (userRoles == null)
                {
                    await user.AddRoleAsync(roleChoice);
                    await channel.SendMessageAsync($"{user.Mention}, welcome to the {channel.Name} channel!");
                }
                else
                {
                    await ReplyAsync($"{user.Mention}, you already have the specified role!");
                }
                return;
            }
            if (roleChoice.Name == "js")
            {
                var roleName = "Coding_JScript";
                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "javascript") as SocketTextChannel;

                var userRoles = user.Roles.FirstOrDefault(has => has.Name.ToUpper() == roleName.ToUpper());
                if (userRoles == null)
                {
                    await user.AddRoleAsync(roleChoice);
                    await channel.SendMessageAsync($"{user.Mention}, welcome to the {channel.Name} channel!");
                }
                else
                {
                    await ReplyAsync($"{user.Mention}, you already have the specified role!");
                }
                return;
            }
            await ReplyAsync($"{Context.User.Mention}, no such role exists!");
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
 