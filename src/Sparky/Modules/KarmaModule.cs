using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Humanizer;
using Sparky.Data;

namespace Sparky.Modules
{
    [Group("rep")]
    public class KarmaModule : SparkyBaseModule
    {
        private readonly KarmaContext _karmaContext;

        public KarmaModule(KarmaContext karmaContext)
            => this._karmaContext = karmaContext;

        [GroupCommand]
        public async Task RepAsync(CommandContext context, DiscordMember user)
        {
            var requesteeInformation = this._karmaContext.GetKarmaInformation(context.User.Id);

            if (!requesteeInformation.CanGiveKarma)
            {
                var difference = requesteeInformation.NextKarmaAt.UtcDateTime - DateTimeOffset.UtcNow.DateTime;
                await context.Message.CreateReactionAsync(DeniedEmoji);
                return;
            }

            var targetInformation = this._karmaContext.GetKarmaInformation(user.Id);

            targetInformation.Modify(x => 
            {
                x.KarmaCount++;
            });

            requesteeInformation.Modify(x =>
            {
                x.NextKarmaIn = TimeSpan.FromDays(1);
                x.LastKarmaGivenAt = DateTimeOffset.UtcNow;
            });

            await context.Message.CreateReactionAsync(AcceptedEmoji);
        }

        [Command("stats")]
        public async Task GetRepStatsAsync(CommandContext context, DiscordMember member = null)
        {
            var requestedUser = member ?? context.User as DiscordMember;

            var requestedInformation = this._karmaContext.GetKarmaInformation(requestedUser.Id);

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder
            {
                Title = $"{requestedUser.Nickname ?? requestedUser.Username}",
                Description = $"**Reputation:** {requestedInformation.KarmaCount}\n" +
                              $"**Last reputation given:** {requestedInformation.LastKarmaGivenAt.Humanize()}\n" +
                              (requestedInformation.CanGiveKarma ? "**Can give reputation**" : $"Can give reputation in **{(DateTimeOffset.UtcNow - requestedInformation.NextKarmaAt.UtcDateTime).Humanize()}**")
            };

            await context.RespondAsync("", embed: builder.Build());
        }
    }
}