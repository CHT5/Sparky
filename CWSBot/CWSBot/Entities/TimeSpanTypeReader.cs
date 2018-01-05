using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace CWSBot.Entities
{
    public class TimeSpanTypeReader : TypeReader
    {
        private const string Pattern = "((?<years>[0-9]+)(years|year|y))?((?<weeks>[0-9]+)(weeks|week|w))?((?<days>[0-9]+)(days|day|d))?((?<hours>[0-9]+)(hours|hour|h))?((?<minutes>[0-9]+)(minutes|minute|min|m))?((?<seconds>[0-9]+)(seconds|second|s))?"; 

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider provider)
        {
            var match = Regex.Match(input, Pattern);

            if (match is null) 
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse TimeSpan"));

            if (!int.TryParse(match.Groups["years"].Value, out int years) && !string.IsNullOrEmpty(match.Groups["years"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for years"));

            if (!int.TryParse(match.Groups["weeks"].Value, out int weeks) && !string.IsNullOrEmpty(match.Groups["weeks"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for weeks"));

            if (!int.TryParse(match.Groups["days"].Value, out int days) && !string.IsNullOrEmpty(match.Groups["days"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for days"));

            if (!int.TryParse(match.Groups["hours"].Value, out int hours) && !string.IsNullOrEmpty(match.Groups["hours"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for hours"));
        
            if (!int.TryParse(match.Groups["minutes"].Value, out int minutes) && !string.IsNullOrEmpty(match.Groups["minutes"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for minutes"));

            if (!int.TryParse(match.Groups["seconds"].Value, out int seconds) && !string.IsNullOrEmpty(match.Groups["seconds"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for seconds"));

            var currentTime = DateTimeOffset.UtcNow;

            var dueTime = currentTime.AddYears(years)
                                     .AddDays(days + weeks*7)
                                     .AddHours(hours)
                                     .AddMinutes(minutes)
                                     .AddSeconds(seconds);

            var timespan = dueTime - currentTime;

            if (timespan.TotalSeconds == 0)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.UnmetPrecondition, "The timespan has to be at least 1 second long!"));

            return Task.FromResult(TypeReaderResult.FromSuccess(timespan));
        }
    }
}