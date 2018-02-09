using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Sparky.Modules
{
    public class HelloWorldModule : BaseCommandModule
    {
        [Command("hello")]
        public async Task Test(CommandContext ctx)
        {
            await ctx.RespondAsync($"Hello, {ctx.User.Mention}");
        }
    }
}