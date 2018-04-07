using System;
using System.Linq;
using Sparky.Data.Attributes;

namespace Sparky.Data
{
    public static class Extensions
    {
        public static bool IsTemporary(this ModerationAction action)
        {
            var name = Enum.GetName(action.GetType(), action);
            var attributes = action.GetType().GetField(name).GetCustomAttributes(false);
            return attributes.Any(x => x is TemporaryModActionAttribute);
        }
    }
}