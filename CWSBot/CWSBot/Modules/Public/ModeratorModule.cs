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

        public ModeratorModule(CommandService service, IConfiguration config, LogContext dctx)
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

        [Command("mute", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task MuteAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            await _dctx.Database.EnsureCreatedAsync();
            // get our muted role
            var muteRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_mute_name"].ToLower());
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
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was muted by {Context.User.Mention}.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low
            };
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            var dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            var logEmbed = new EmbedBuilder
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

        [Command("warn", RunMode = RunMode.Async)] 
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task WarnAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        { 
            await _dctx.Database.EnsureCreatedAsync();
            // get our warned role
            var warnRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_warned_name"].ToLower());
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
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was warned by {Context.User.Mention}.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null, 
                Severity = Severity.Low
            };
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            var dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            var logEmbed = new EmbedBuilder
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

        [Command("kick", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
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
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was kicked by {Context.User.Mention}.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null, 
                Severity = Severity.Medium
            };

            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            var dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            var logEmbed = new EmbedBuilder
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

        [Command("ban", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
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
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was banned by {Context.User.Mention}.",
                Time = DateTime.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Severe
            };
            // add and save changes
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            var dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            var logEmbed = new EmbedBuilder
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

        [Command("reason", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task ReasonAsync(int id, [Remainder] string reason)
        {
            // try and get our log from db
            var dbLog = _dctx.Modlogs.FirstOrDefault(log => log.Id == id);
            // if it is null, lets let the user know that they used the wrong id!
            if (dbLog is null)
            {
                await ReplyAsync("Sorry, that doesn't appear to be a valid id.");
                return;
            }
            // get the message from our logging channel
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());

            ulong messageId = dbLog.MessageId ?? 0;

            var oldMessage = await logChannel.GetMessageAsync(messageId);

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

            var logEmbed = new EmbedBuilder
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
    }
}
