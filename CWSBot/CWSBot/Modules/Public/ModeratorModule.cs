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
        
        [Command("kick")]
        [Summary("Kicks someone from the guild.")]
        public async Task Kick(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var contextUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!contextUser.GuildPermissions.KickMembers)
            {
                await ReplyAsync("Nice try.");
            }
            else
            {
                // sends dm to the banned user, logs the kick, kicks the user
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync($"You have been kicked from {Context.Guild.Name}.\n" +
                    $"Reason: {reason}\n" +
                    $"If you feel you were mistakenly kicked, contact a staff member.");
                await user.KickAsync();
                SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");
                await logChannel.SendMessageAsync($"```ini\n" +
                    $"[{contextUser}] kicked [{user}]. Reason: [{reason}]\n" +
                    $"```");
            }
        }

        [Command("ban")]
        [Summary("Bans someone from the guild.")]
        public async Task Ban(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var contextUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!contextUser.GuildPermissions.KickMembers)
            {
                await ReplyAsync("Nice try.");
            }
            else
            {
                // sends dm to the banned user, logs the ban, bans the user
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync($"You have been kicked from {Context.Guild.Name}.\n" +
                    $"Reason: {reason}\n" +
                    $"If you feel you were mistakenly banned, contact a staff member.");
                await Context.Guild.AddBanAsync(user, 7, reason);
                SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");
                await logChannel.SendMessageAsync($"```ini\n" +
                    $"[{contextUser}] banned [{user}]. Reason: [{reason}]\n" +
                    $"```");
            }
        }

        [Command("softban")]
        [Summary("Deletes 1 day worth of messages from a misbehaving user.")]
        public async Task Softban(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var contextUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!contextUser.GuildPermissions.KickMembers)
            {
                await ReplyAsync("Nice try.");
            }
            else
            {
                // sends dm to the softbanned user, logs the softban, softbans the user
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync($"You have been softbanned from {Context.Guild.Name}, feel free to rejoin.\n" +
                    $"Reason: {reason}\n" +
                    $"If you feel you were mistakenly softbanned, contact a staff member.");
                await Context.Guild.AddBanAsync(user, 1);
                await Context.Guild.RemoveBanAsync(user);
                SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");
                await logChannel.SendMessageAsync($"```ini\n" +
                    $"[{contextUser}] softbanned [{user}]. Reason: [{reason}]\n" +
                    $"```");
            }
        }
    }
}
