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
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;

        public ModeratorModule(CommandService service)             // Create a constructor for the commandservice dependency
        {
            _service = service;
        }

        [Command("prune")] //Command Name
        [Remarks("removes a certain amount of messages")] //Summary for your command. it will not add anything.
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PruneMessages(int prune = 0)
        {
            var guild = Context.Client.GetGuild(351284764352839690);
            var user = guild.GetUser(Context.User.Id);

            if (prune == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, please add the amount of messages to be pruned!\n *!prune x*");
            }
            else
            {
                var items = await Context.Channel.GetMessagesAsync(prune).Flatten();
                await Context.Channel.DeleteMessagesAsync(items);

                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "mod-logs") as SocketTextChannel;

                await channel.SendMessageAsync($"```ini\n" +
                    $"[{Context.User.Username}] pruned [{prune}] message(s) in [{Context.Channel.Name}]```");
            }
        }

        [Command("pruneuser")] //Command Name
        [Remarks("removes a certain amount of messages")] //Summary for your command. it will not add anything.
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PruneUser(IUser user, int prune = 0)
        {
            var guild = Context.Client.GetGuild(351284764352839690);

            if (prune == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, please add the amount of messages to be pruned!\n *!prune x*");
            }
            else
            {
                var Items = await Context.Channel.GetMessagesAsync().Flatten();
                var usermessages = Items.Where(x => x.Author == user).Take(prune);
                await Context.Channel.DeleteMessagesAsync(usermessages);

                var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "mod-logs") as SocketTextChannel;
                await channel.SendMessageAsync($"```ini\n" +
                    $"[{Context.User.Username}] pruned [{prune}] message(s) in [{Context.Channel.Name}] from [{user}]```");
            }
        }

        [Command("warn")]
        [Remarks("warns a specified user and deducts point(s) of rep.")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task WarnUser(IUser user = null, [Remainder] String reason = null)
        {
            //PARAMETER STUFF
            if(user == null)
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a user and a reason for warning!");
                return;
            }
            if (reason == null)
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a reason for the warning!!");
                return;
            }

            //DATABASE STUFF
            var statusResult = Database.GetUserStatus(user);
            var karmaReduction = (statusResult.FirstOrDefault().warningCount) * 1;
            if (karmaReduction == 0)
            {
                karmaReduction = 1;
            }
            Database.WarnUser(user, karmaReduction);

            //MODLOG STUFF
            var guild = Context.Client.GetGuild(351284764352839690);
            var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "mod-logs") as SocketTextChannel;
            await channel.SendMessageAsync($"```ini\n" +
                $"[{Context.User.Username}] warned [{user.Username}], reason: [{reason}], resulting in the loss of [-{karmaReduction}] karma```");
        }
        
        [Command("kick", RunMode = RunMode.Async)]
        [Summary("Kicks someone off the guild.")]
        public async Task kick(IGuildUser user, [Remainder] string reason)
        {
            var Bot = await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id);
            var GuildUser = await Context.Guild.GetUserAsync(Context.User.Id);
            if (!GuildUser.GuildPermissions.KickMembers)
            {
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync("Nice try.");
            }
            else if (!Bot.GuildPermissions.KickMembers)
            {
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync("Nice try, bot doesn't have the required permissions.");
            }
            else
            {
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync(user.Mention + " has been kicked.");
                var reasondm = await user.GetOrCreateDMChannelAsync();
                await reasondm.SendMessageAsync("You have been kicked from " + Context.Guild.Name + " for "
                    + reason + ". If you feel the kick was made in mistake, contact a staff member.");
                await user.KickAsync(reason);
                SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");
                await logChannel.SendMessageAsync("**" + user.Username + "** is kicked by **" + Context.User.Username + "** with the reason **" + reason + "**.");
            }
        }

        [Command("ban", RunMode = RunMode.Async)]
        [Summary("Bans someone off the guild.")]
        public async Task ban(IGuildUser user, [Remainder] string reason)
        {
            var Bot = await Context.Guild.GetUserAsync(Context.Client.CurrentUser.Id);
            var GuildUser = await Context.Guild.GetUserAsync(Context.User.Id);
            if (!GuildUser.GuildPermissions.BanMembers)
            {
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync("Nice try.");
            }
            else if (!Bot.GuildPermissions.BanMembers)
            {
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync("Nice try, bot doesn't have the required permissions.");
            }
            else
            {
                var guild = Context.Guild;
                var channel = await GuildUser.GetOrCreateDMChannelAsync();
                await channel.SendMessageAsync(user.Mention + " has been banned.");
                var reasondm = await user.GetOrCreateDMChannelAsync();
                await reasondm.SendMessageAsync("You have been banned from " + Context.Guild.Name + " for "
                    + reason + ". If you feel the ban was made in mistake, contact a staff member.");
                await guild.AddBanAsync(user, 7, reason);
                SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");
                await logChannel.SendMessageAsync("**" + user.Username + "** is banned by **" + Context.User.Username + "** with the reason **" + reason + "**.");
            }
        }
    }
}
