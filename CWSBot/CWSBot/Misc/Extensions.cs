using System;
using System.Collections.Generic;

namespace CWSBot.Misc
{
    public static class Extensions
    {
        public static string GetHumanizedString(this TimeSpan input)
        {
            List<string> strings = new List<string>();

            if (input.Days > 0)
                strings.Add($"{input.Days} {(input.Days == 1 ? "day" : "days")}");
            
            if (input.Hours > 0)
                strings.Add($"{input.Hours} {(input.Hours == 1 ? "hour" : "hours")}");

            if (input.Minutes > 0)
                strings.Add($"{input.Minutes} {(input.Minutes == 1 ? "minute" : "minutes")}");
            
            if (input.Seconds > 0)
                strings.Add($"{(strings.Count > 0 ? "and " : string.Empty)}{input.Seconds} {(input.Seconds == 1 ? "second" : "seconds")}");

            return string.Join(" ", strings);
        }
    }
}