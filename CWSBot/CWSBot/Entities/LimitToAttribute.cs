using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;

namespace CWSBot.Entities
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class LimitToAttribute : ParameterPreconditionAttribute
    {
        private int _maxTextElements;

        private int? _maxChars;

        /// <summary>
        ///     The maximum number of <see cref="char"/> in the text.
        ///     Will only be checked if set.
        /// </summary>
        public int MaxTotalChars { get => _maxChars ?? 0; set => _maxChars = value; }

        /// <summary>
        /// <param name="maxUniLength">
        ///     The maximum number of unicode text elements in the text.
        /// </param>
        /// </summary>
        public LimitToAttribute(int maxUniLength)
            => this._maxTextElements = maxUniLength;

        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
        {
            if (!(value is string rawStringValue)) throw new InvalidOperationException($"The {nameof(LimitToAttribute)} can only be used on {nameof(String)}");

            var stringInfo = new StringInfo(rawStringValue);

            if (this._maxTextElements < stringInfo.LengthInTextElements || 
                this._maxChars.HasValue && this._maxChars.Value < rawStringValue.Length)
            {
                await context.Channel.SendMessageAsync($"The input for {parameter.Name} has to be shorter or equal to {this._maxTextElements} characters!"); // Let the user know what happened
                return PreconditionResult.FromError("Too long input string");
            }

            return PreconditionResult.FromSuccess();
        }
    }
}