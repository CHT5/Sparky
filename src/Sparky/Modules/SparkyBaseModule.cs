using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;

namespace Sparky.Modules
{
    public abstract class SparkyBaseModule : BaseCommandModule
    {
        protected DiscordEmoji AcceptedEmoji { get; private set; }

        protected DiscordEmoji DeniedEmoji { get; private set; }

        public override Task BeforeExecutionAsync(CommandContext ctx)
        {
            this.AcceptedEmoji = DiscordGuildEmoji.FromName(ctx.Client, ":accepted:");
            this.DeniedEmoji = DiscordGuildEmoji.FromName(ctx.Client, ":rejected:");
            return Task.CompletedTask;
        }
    }
}