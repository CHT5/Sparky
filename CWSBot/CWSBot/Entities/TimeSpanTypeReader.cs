using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.Commands;

namespace CWSBot.Entities
{
    public class TimeSpanTypeReader : TypeReader
    {
        private readonly string _pattern = "((?<hours>[0-9]+)(hours|hour|h))?((?<minutes>[0-9]+)(minutes|minute|min|m))?((?<seconds>[0-9]+)(seconds|second|s))?"; 

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider provider)
        {
            var _match = Regex.Match(input, _pattern);

            if (_match is null) 
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Failed to parse TimeSpan"));
        
            if (!int.TryParse(_match.Groups["hours"].Value, out int _hours) && !string.IsNullOrEmpty(_match.Groups["hours"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for hours"));
        
            if (!int.TryParse(_match.Groups["minutes"].Value, out int _minutes) && !string.IsNullOrEmpty(_match.Groups["minutes"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for minutes"));

            if (!int.TryParse(_match.Groups["seconds"].Value, out int _seconds) && !string.IsNullOrEmpty(_match.Groups["seconds"].Value))
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Too large number given for seconds"));
            
            var _timespan = new TimeSpan(_hours, _minutes, _seconds);

            if (_timespan.TotalSeconds == 0)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.UnmetPrecondition, "The timespan has to be at least 1 second long!"));

            return Task.FromResult(TypeReaderResult.FromSuccess(_timespan));
        }
    }
}