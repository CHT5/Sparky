using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Discord;
using Discord.WebSocket;

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

        // This will return green if it's not a GuildUser
        public static Color GetRoleColor(this IUser user)
            => (user as SocketGuildUser)?.Roles.Where(x => x.Color.RawValue != Color.Default.RawValue)
                                               .OrderByDescending(x => x.Position)
                                               .FirstOrDefault()?.Color ?? Color.Green;

        public static bool PointsToImage(this Uri uri)
        {
            if (!Path.HasExtension(uri.AbsoluteUri)) return false;

            var path = String.Format("{0}{1}{2}{3}", uri.Scheme, Uri.SchemeDelimiter, uri.Authority, uri.AbsolutePath);

            var ext = Path.GetExtension(path);

            switch(ext)
            {
                case ".png":
                case ".webp":
                case ".gif":
                case ".jpg":
                case ".jpeg":
                    return true;

                default:
                    return false;
            }
        }
    }
}