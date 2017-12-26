using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Configuration;
using CWSBot.Entities;
using CWSBot.Interaction;
using Discord.Rest;

namespace CWSBot.Modules.Public
{
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;
        private IConfiguration _config;
        private LogContext _dctx;
        public ModeratorModule(CommandService service, IConfiguration config, LogContext dctx)             // Create a constructor for the commandservice dependency
        {
            _service = service;
            _config = config;
            _dctx = dctx;
        }
        #region Chat Cleaning Commands
        [Command("prune")] //Command Name
        [Remarks("removes a certain amount of messages")] //Summary for your command. it will not add anything.
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PruneMessages(int prune = 0)
        {
            if (prune == 0)
                await ReplyAsync($"{Context.User.Mention}, please add the amount of messages to be pruned!\n *!prune x*");
            else
            {
                var messages = await Context.Channel.GetMessagesAsync(prune).Flatten();
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(messages);

                var channel = Context.Guild.Channels.FirstOrDefault(xc => xc.Name == "mod_logs") as SocketTextChannel;
                await channel.SendMessageAsync($"```ini\n" +
                    $"[{Context.User.Username}] pruned [{prune}] message(s) in [{Context.Channel.Name}]```");
            }
        }

        [Command("pruneuser")] //Command Name
        [Remarks("removes a certain amount of messages")] //Summary for your command. it will not add anything.
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PruneUser(IUser user, int prune = 0)
        {
            if (prune == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, please add the amount of messages to be pruned!\n *!prune x*");
            }
            else
            {
                var messages = await Context.Channel.GetMessagesAsync().Flatten();
                var usermessages = messages.Where(x => x.Author == user).Take(prune);
                await (Context.Channel as ITextChannel).DeleteMessagesAsync(usermessages);

                var channel = Context.Guild.Channels.FirstOrDefault(xc => xc.Name == "mod_logs") as SocketTextChannel;
                await channel.SendMessageAsync($"```ini\n" +
                    $"[{Context.User.Username}] pruned [{prune}] message(s) in [{Context.Channel.Name}] from [{user}]```");
            }
        }
        #endregion

        #region Moderation Commands (logged)
        [Command("mute", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            await _dctx.Database.EnsureCreatedAsync();
            // get our muted role
            SocketRole muteRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_mute_name"].ToLower());
            // check to make sure our target doesn't already have the role
            if (targetUser.Roles.Any(role => role.Name.ToLower() == muteRole.Name.ToLower()))
            {
                await ReplyAsync("User is already muted!");
                return;
            }
            // try adding the role to our target. In the case of a 403, let the user know.
            try
            {
                await targetUser.AddRoleAsync(muteRole);
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't mute your target user. Perhaps their role is higher than mine?");
                return;
            }
            // get our logging channel
            SocketTextChannel logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            ModLog modLog = new ModLog
            {
                Action = $"{ targetUser.Mention } was muted by { Context.User.Mention }.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low
            };
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            ModLog dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            EmbedBuilder logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")}\n\n" +
                $"Reason: {reason ?? _config["prefix"] + $"reason {dbLog.Id}"}",
                Color = Color.Orange
            };
            // send it off.
            var message = await logChannel.SendMessageAsync("", false, logEmbed.Build());

            dbLog.MessageId = message.Id;
            await _dctx.SaveChangesAsync();
        }

        [Command("warn", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task WarnAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        { 
            await _dctx.Database.EnsureCreatedAsync();
            // get our warned role
            SocketRole warnRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_warned_name"].ToLower());
            // check to make sure our target doesn't already have the role
            if (targetUser.Roles.Any(role => role.Name.ToLower() == warnRole.Name.ToLower()))
            {
                await ReplyAsync("User is already warned!");
                return;
            }
            // try adding the role to our target. In the case of a 403, let the user know.
            try
            {
                await targetUser.AddRoleAsync(warnRole);
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't add the role to your target user. Perhaps their role is higher than mine?");
                return;
            }
            // get our logging channel
            SocketTextChannel logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            ModLog modLog = new ModLog
            {
                Action = $"{ targetUser.Mention } was warned by { Context.User.Mention }.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null, 
                Severity = Severity.Low
            };
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
             ModLog dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            EmbedBuilder logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")}\n\n" +
                $"Reason: {reason ?? _config["prefix"]+ $"reason {dbLog.Id}"}",
                Color = Color.Orange
            };
            // send it off.
            var message = await logChannel.SendMessageAsync("", false, logEmbed.Build());

            dbLog.MessageId = message.Id;
            await _dctx.SaveChangesAsync();
        }

        [Command("kick", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task KickAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            // try kicking our target
            try
            {
                await targetUser.KickAsync();
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't kick your target user. Perhaps their role is higher than mine?");
                return;
            }
            // get our logging channel
            SocketTextChannel logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            ModLog modLog = new ModLog
            {
                Action = $"{ targetUser.Mention } was kicked by { Context.User.Mention }.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null, 
                Severity = Severity.Medium
            };

            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            ModLog dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            EmbedBuilder logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")}\n\n" +
                $"Reason: {reason ?? _config["prefix"] + $"reason {dbLog.Id}"}",
                Color = Color.DarkOrange
            };
            // send it off.
            var message = await logChannel.SendMessageAsync("", false, logEmbed.Build());

            dbLog.MessageId = message.Id;
            await _dctx.SaveChangesAsync();
        }

        [Command("ban", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        { 
            // try banning our target
            try
            {
                await Context.Guild.AddBanAsync(targetUser);
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't ban your target user. Perhaps their role is higher than mine?");
                return;
            }
            // get our logging channel
            SocketTextChannel logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            ModLog modLog = new ModLog
            {
                Action = $"{ targetUser.Mention } was banned by { Context.User.Mention }.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Severe
            };
            // add and save changes
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            ModLog dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            EmbedBuilder logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")}\n\n" +
                $"Reason: {reason ?? _config["prefix"] + $"reason {dbLog.Id}"}",
                Color = Color.Red
            };
            // send it off.
            var message = await logChannel.SendMessageAsync("", false, logEmbed.Build());

            dbLog.MessageId = message.Id;
            await _dctx.SaveChangesAsync();
        }

        [Command("reason", RunMode = RunMode.Async), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task ReasonAsync(int id, [Remainder] string reason)
        {
            // try and get our log from db
            ModLog dbLog = _dctx.Modlogs.FirstOrDefault(log => log.Id == id);
            // if it is null, lets let the user know that they used the wrong id!
            if (dbLog is null)
            {
                await ReplyAsync("Sorry, that doesn't appear to be a valid id.");
                return;
            }
            // get the message from our logging channel
            SocketTextChannel logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());

            ulong messageId = dbLog.MessageId ?? 0;

            IMessage oldMessage = await logChannel.GetMessageAsync(messageId);

            // configure our embed color to make sure it doesn't get changed.

            Color embedColour;

            switch (dbLog.Severity)
            {
                case (Severity.Severe):
                    embedColour = Color.Red;
                    break;
                case (Severity.Medium):
                    embedColour = Color.DarkOrange;
                    break;
                default:
                    embedColour = Color.Orange;
                    break;
            }

            EmbedBuilder logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")}\n\n" +
                $"Reason: {reason}",
                Color = embedColour
            };

            await (oldMessage as RestUserMessage).ModifyAsync(msg => msg.Embed = logEmbed.Build());

            await Context.Message.DeleteAsync();
        }

        #endregion

        #region old, not so great, code
        /*[Command("warn", RunMode = RunMode.Async)]
        [Summary("Warns a specified user, and kicks if user has a certain role when adding a warn.")]
        public async Task warnSystem(IGuildUser user, [Remainder] string reason)
        {
            SocketTextChannel logChannel = (Context.Guild as SocketGuild).TextChannels.FirstOrDefault(x => x.Name == "mod_logs");

            await Context.Message.DeleteAsync();

            var GuildUser = (Context.Guild as SocketGuild).GetUser(Context.User.Id);
            if (!GuildUser.GuildPermissions.KickMembers)
            {
                //await Context.Channel.SendMessageAsync("Sorry, but Sparky finds you do not have enough permissions to warn users.");
                return;
            }

            SocketGuildUser guildUser = user as SocketGuildUser;
            if (guildUser.Roles.Any(r => r.Name == "Yellow Card Issued!"))
            {
                await user.KickAsync(reason);
                await logChannel.SendMessageAsync($"```\n [{user.Username}] has been kicked for [{reason}] while having Yellow Card Issued!```");
            }
            else if (guildUser.Roles.Any(r => r.Name == "Warning Issued!"))
            {
                await user.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name == "Yellow Card Issued!"));
                await logChannel.SendMessageAsync($"```ini\n Yellow Card Issued! role added to [{user.Username}] for [{reason}].```");
            }
            else
            {
                await user.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(x => x.Name == "Warning Issued!"));
                await logChannel.SendMessageAsync($"```ini\n Warning Issued! role added to [{user.Username}] for [{reason}].```");
            }
        }
        
        [Command("kick")]
        [Summary("Kicks someone from the guild.")]
        public async Task Kick(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var guildUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!guildUser.GuildPermissions.KickMembers)
                await ReplyAsync("Nice try.");
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
                    $"[{guildUser}] kicked [{user}]. Reason: [{reason}]```");
            }
        }

        [Command("ban")]
        [Summary("Bans someone from the guild.")]
        public async Task Ban(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var guildUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!guildUser.GuildPermissions.KickMembers)
                await ReplyAsync("Nice try.");
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
                    $"[{guildUser}] banned [{user}]. Reason: [{reason}]```");
            }
        }

        [Command("softban")]
        [Summary("Deletes 1 day worth of messages from a misbehaving user.")]
        public async Task Softban(IGuildUser user, [Remainder] string reason)
        {
            // stores context user as a guild user
            var guildUser = Context.Guild.GetUser(Context.User.Id);
            // check context users perms
            if (!guildUser.GuildPermissions.KickMembers)
                await ReplyAsync("Nice try.");
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
                    $"[{guildUser}] softbanned [{user}]. Reason: [{reason}]```");
            }
        }*/
        #endregion
    }

    public enum Severity
    {
        Low, 
        Medium, 
        Severe
    }
}
