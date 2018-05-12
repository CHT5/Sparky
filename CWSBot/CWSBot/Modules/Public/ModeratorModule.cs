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
using Humanizer;
using Humanizer.Localisation;
using CWSBot.Services;

namespace CWSBot.Modules.Public
{
    public class ModeratorModule : ModuleBase<SocketCommandContext>
    {
        private CommandService _service;
        private IConfiguration _config;
        private LogContext _dctx;
        private MuteService _mutes;

        public ModeratorModule(CommandService service, IConfiguration config, LogContext dctx, MuteService mutes)
        {
            _service = service;
            _config = config;
            _dctx = dctx;
            _mutes = mutes;
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
        public async Task MuteAsync(SocketGuildUser targetUser, TimeSpan timeToMute, [Remainder] string reason = null)
        {
            // get our muted role
            var muteRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_mute_name"].ToLower());
            // check to make sure our target doesn't already have an unmute queued
            var timeToUnmute = DateTimeOffset.UtcNow + timeToMute;
            if (!_mutes.TryAddMute(Context.User, targetUser, Context.Guild, timeToUnmute))
            {
                await ReplyAsync($"User is already muted.");
            }
            // try adding the role to our target. In the case of a 403, let the user know.
            try
            {
                await targetUser.AddRoleAsync(muteRole);
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't mute your target user. Perhaps their role is higher than mine?");
                // remove the queued unmute since we couldn't assign the role.
                _mutes.RemoveQueuedUnmute(targetUser);
                return;
            }


            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was muted by {Context.User.Mention} for {timeToMute.Humanize(5, maxUnit: TimeUnit.Year, minUnit: TimeUnit.Second)}",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low,
                ActorId = Context.User.Id
            };

            await MakeLogAsync(modLog, reason);
        }

        [Command("unmute", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnmuteAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            await _mutes.ForceUnmute(targetUser);

            var log = new ModLog
            {
                Action = $"{targetUser.Mention} was unmuted by {Context.User.Mention}",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low,
                ActorId = Context.User.Id
            };

            await MakeLogAsync(log, reason);
        }

        [Command("warn", RunMode = RunMode.Async)] 
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task WarnAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        { 
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
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was warned by {Context.User.Mention}.",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null, 
                Severity = Severity.Low, 
                ActorId = Context.User.Id
            };

            await MakeLogAsync(modLog, reason);
        }

        [Command("unwarn", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task UnwarnAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            // get our warned role
            var warnRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_warned_name"].ToLower());
            // check to make sure our target has
            if (!targetUser.Roles.Any(role => role.Name.ToLower() == warnRole.Name.ToLower()))
            {
                await ReplyAsync("That user isn't warned!");
                return;
            }
            // try adding the role to our target. In the case of a 403, let the user know.
            try
            {
                await targetUser.RemoveRoleAsync(warnRole);
            }
            catch
            {
                await ReplyAsync("Sorry, looks like I couldn't unwarn your target user. Perhaps their role is higher than mine?");
                return;
            }
            // build our database entry and add it to the database
            var modLog = new ModLog
            {
                Action = $"{targetUser.Mention} was unwarned by {Context.User.Mention}.",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low,
                ActorId = Context.User.Id
            };

            await MakeLogAsync(modLog, reason);
        }

        [Command("kick", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task KickAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        {
            if (Context.User.Id == targetUser.Id)
                await ReplyAsync("Kicking yourself is dumb, and you should feel dumb.");
            
            else if (Context.Member.Hierarchy <= targetUser.Hierarchy)
                await ReplyAsync("You don't have the the necessary permissions.");
            
            else if (Context.CurrentMember.Hierarchy <= targetUser.Hierarchy){
                await ReplyAsync("I don't have the necessary permissions.");
            }
            
            else
            {
                await targetUser.KickAsync();
                
                // build our database entry and add it to the database
                var modLog = new ModLog
                {
                    Action = $"{targetUser.Mention} was kicked by {Context.User.Mention}.",
                    Time = DateTimeOffset.Now,
                    Reason = reason ?? "n/a",
                    MessageId = null, 
                    Severity = Severity.Medium,
                    ActorId = Context.User.Id
                };

                await MakeLogAsync(modLog, reason);
            }
        }

        [Command("ban", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task BanAsync(SocketGuildUser targetUser, [Remainder] string reason = null)
        { 
            if (Context.User.Id == targetUser.Id)
                await ReplyAsync("Banning yourself is dumb, and you should feel dumb.");
            
            else if (Context.Member.Hierarchy <= targetUser.Hierarchy)
                await ReplyAsync("You don't have the the necessary permissions.");
            
            else if (Context.CurrentMember.Hierarchy <= targetUser.Hierarchy){
                await ReplyAsync("I don't have the necessary permissions.");
            }
            
            else
            {
                await Context.Guild.AddBanAsync(targetUser);
                
                // build our database entry and add it to the database
                var modLog = new ModLog
                {
                    Action = $"{targetUser.Mention} was banned by {Context.User.Mention}.",
                    Time = DateTimeOffset.Now,
                    Reason = reason ?? "n/a",
                    MessageId = null,
                    Severity = Severity.Severe,
                    ActorId = Context.User.Id
                };

                await MakeLogAsync(modLog, reason);
            }
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

            dbLog.Reason = reason;
            _dctx.SaveChanges();

            await UpdateLogAsync(dbLog, reason);
        }

        [Command("audit", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task AuditAsync(int caseId)
        {
            if (!_dctx.Modlogs.Any(log => log.Id == caseId))
            {
                await ReplyAsync("I have no case files stored with that id.");
                return;
            }

            var dbLog = _dctx.Modlogs.FirstOrDefault(log => log.Id == caseId);

            var actor = Context.Client.GetUser(dbLog.ActorId);

            var auditEmbed = new EmbedBuilder
            {
                Title = $"Audit of case {dbLog.Id}",
                Description = $"Responsible staff member: {actor.Mention}\n" +
                $"Action: {dbLog.Action}\n" +
                $"Reason: {dbLog.Reason}\n",
                Footer = (new EmbedFooterBuilder { Text = $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")} GMT+0" })
            };

            await ReplyAsync("", false, auditEmbed.Build());
        }

        [Command("nick", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public Task NickAsync(SocketGuildUser target, [Remainder] string reason = null)
        {
            var modLog = new ModLog
            {
                Action = $"{target.Mention} nickname was changed by {Context.User.Mention}.",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low,
                ActorId = Context.User.Id
            };

            return MakeLogAsync(modLog, reason);
        }

        [Command("authbot", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public Task AuthBotAsync(SocketGuildUser target, SocketGuildUser bot, [Remainder] string reason = null)
        {
            var modLog = new ModLog
            {
                Action = $"{target.Mention}'s bot {bot.Mention} was authed by {Context.User.Mention}.",
                Time = DateTimeOffset.Now,
                Reason = reason ?? "n/a",
                MessageId = null,
                Severity = Severity.Low,
                ActorId = Context.User.Id
            };

            return MakeLogAsync(modLog, reason);
        }

        #endregion

        #region Helper Methods

        private async Task UpdateLogAsync(ModLog dbLog, string reason)
        {
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
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")} GMT+0\n\n" +
                $"Reason: {reason}",
                Color = embedColour,
                Footer = (new EmbedFooterBuilder { Text = $"Case {dbLog.Id}" })
            };

            await (oldMessage as RestUserMessage).ModifyAsync(msg => msg.Embed = logEmbed.Build());

            await Context.Message.DeleteAsync();
        }

        private async Task MakeLogAsync(ModLog modLog, string reason = null)
        {
            // Get our logging channel.
            var logChannel = Context.Guild.TextChannels.FirstOrDefault(channel => channel.Name.ToLower() == _config["moderation_log_channel"].ToLower());

            // Define our embed color based on log severity.
            Color embedColour;

            switch (modLog.Severity)
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

            // add our log and save changes
            _dctx.Add(modLog);
            await _dctx.SaveChangesAsync();
            // grab the entry so we can get the id
            var dbLog = _dctx.Modlogs.LastOrDefault();
            // build our embed
            var logEmbed = new EmbedBuilder
            {
                Title = $"Log Message",
                Description = dbLog.Action + "\n\n" +
                $"Time: {dbLog.Time.ToString("HH:mm:ss dd/MM/yyy")} GMT+0\n\n" +
                $"Reason: {reason ?? _config["prefix"] + $"reason {dbLog.Id} to set a reason."}",
                Color = embedColour,
                Footer = (new EmbedFooterBuilder { Text = $"Case {dbLog.Id}" } )
            };
            // send it off.
            var message = await logChannel.SendMessageAsync("", false, logEmbed.Build());
            // update the modlog with the id of the message that it refers to, so we can use the reason command later on.
            dbLog.MessageId = message.Id;
            await _dctx.SaveChangesAsync();
        }
        #endregion
    }
}
