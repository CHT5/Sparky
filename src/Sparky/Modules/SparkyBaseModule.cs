using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Sparky.Modules
{
    public abstract class SparkyBaseModule : BaseCommandModule
    {
        protected DiscordEmoji AcceptedEmoji { get; private set; }

        protected DiscordEmoji DeniedEmoji { get; private set; }

        protected ILogger Logger { get; private set; }

        protected IConfiguration Config { get; private set;}

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            Logger = ctx.Services.GetService<ILogger<SparkyBaseModule>>();
            Config = ctx.Services.GetService<IConfiguration>();
            Logger.LogInformation($"Executing {ctx.Command.QualifiedName} for {ctx.User.Username}#{ctx.User.Discriminator}");
            try
            {
                this.AcceptedEmoji = DiscordGuildEmoji.FromName(ctx.Client, Config["emotes:accepted_emoji_name"]);
                this.DeniedEmoji = DiscordGuildEmoji.FromName(ctx.Client, Config["emotes:denied_emoji_name"]);
            }
            catch // Some bad boye removed the emotes
            {
                this.AcceptedEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:");
                this.DeniedEmoji = DiscordEmoji.FromName(ctx.Client, ":x:");
            }
            return Task.CompletedTask;
        }

        public override Task AfterExecutionAsync(CommandContext ctx)
        {
            Logger.LogInformation($"Executed {ctx.Command.QualifiedName} for {ctx.User.Username}#{ctx.User.Discriminator}");
            return Task.CompletedTask;
        }
    }
}