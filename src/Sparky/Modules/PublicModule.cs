using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Configuration;

namespace Sparky.Modules
{
    public class PublicModule : SparkyBaseModule
    {
        private readonly IConfiguration _config;

        public PublicModule(IConfiguration config)
            => this._config = config;

        [Command("info")]
        public async Task GetInfoAsync(CommandContext context)
        {
            TimeSpan uptime;

            using (var process = Process.GetCurrentProcess())
                uptime = DateTimeOffset.Now - process.StartTime;

            var appInfo = context.Client.CurrentApplication;
            var userInfo = context.Guild?.CurrentMember ?? context.Client.CurrentUser;
            var embed = new DiscordEmbedBuilder().WithAuthor(userInfo.Username, icon_url: userInfo.AvatarUrl)
                                                 .WithColor(DiscordColor.MidnightBlue)
                                                 .WithDescription($"**Library**: DSharpPlus v{context.Client.VersionString}\n"+
                                                                  $"**Uptime**: {uptime.Humanize()}\n"+
                                                                  $"For issues with, or questions about the bot, please refer to the staff");
            await context.RespondAsync(embed: embed.Build());
        }

        [Command("clean")]
        [RequireBotPermissions(Permissions.ManageMessages)]
        public async Task CleanMessagesAsync(CommandContext context)
        {
            var messages = await context.Channel.GetMessagesAsync();

            bool HasPrefixOrMention(DiscordMessage message)
            {
                var content = message.Content;
                var id = context.Client.CurrentUser.Id;
                var argPos = 0;

                bool ContentStartsWith(string arg)
                {
                    if (content.StartsWith(arg))
                    {
                        argPos = arg.Length;
                        return true;
                    }

                    return false;
                }

                if (ContentStartsWith(_config["prefix"]) || ContentStartsWith($"<@{id}>") || ContentStartsWith($"<@!{id}>"))
                {
                    var substring = content.Substring(argPos).TrimStart();
                    // check if the substring is any registered command
                    if (context.CommandsNext.RegisteredCommands.Values.Any(x => substring.ToLowerInvariant().StartsWith(x.QualifiedName.ToLowerInvariant())))
                        return true;
                }

                return false;
            }

            var results = messages.Where(x => x.Author.Id == context.Client.CurrentUser.Id || HasPrefixOrMention(x))
                                  .Where(x => (DateTimeOffset.UtcNow - x.CreationTimestamp) < TimeSpan.FromDays(14))
                                  .Where(x => x.Id != context.Message.Id);

            if (results.Count() > 0)
            {
                await context.Channel.DeleteMessagesAsync(results);
                await context.Message.CreateReactionAsync(AcceptedEmoji);
                return;
            }
            
            await context.Message.CreateReactionAsync(DeniedEmoji);
        }
    }
}