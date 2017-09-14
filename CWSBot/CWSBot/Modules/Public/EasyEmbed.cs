using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.Linq;

namespace CWSBot.Modules.Public
{
    public class EasyEmbed
    {
        EmbedBuilder embed;
        EmbedFooterBuilder footer;

        // sets the colour list
        public enum EmbedColour : int
        {
            Random = 0,
            Blue = 0x0000FF,
            Green = 0x00FF00,
            Aqua = 0x00FFFF,
            Red = 0xFF0000,
            Purple = 0x7F00FF,
            Yellow = 0xFFFF00,
            White = 0xFFFFFF,
            Gray = 0xA0A0A0,
            Black = 0x010101,
            LightGreen = 0x99FF99,
            LightAqua = 0x99FFFF,
            LightRed = 0xFF9999,
            LightPurple = 0xCC99FF,
            LightYellow = 0xFFFF99
        };

        //CREATING A BASIC EMBED!
        public void CreateBasicEmbed(EmbedColour colour, string title = null, string description = null, string thumbnailurl = null)
        {
            var setColour = GetEmbedColour(colour);
            embed.WithColor(setColour);
            if (title != null && title != "none") { embed.WithTitle(title); }
            if (description != null && description != "none") { embed.WithDescription(description); }
            if (thumbnailurl != null && thumbnailurl != "none") { embed.WithThumbnailUrl(thumbnailurl); }
        }

        //CREATING A BASIC EMBED WITH FOOTER SUPPORT!
        public void CreateFooterEmbed(EmbedColour colour, string title = null, string description = null, string thumbnailurl = null, string footer_text = null, string footer_thumbnail = null)
        {
            var setColour = GetEmbedColour(colour);
            embed.WithColor(setColour);

            if (title != null && title != "none") { embed.WithTitle(title); }
            if (description != null && description != "none") { embed.WithDescription(description); }
            if (thumbnailurl != null && thumbnailurl != "none") { embed.WithThumbnailUrl(thumbnailurl); }

            footer = new EmbedFooterBuilder();

            if (footer_text != null && footer_text != "none")
            {
                embed.WithFooter(footer
                     .WithText(footer_text)
                );
            }
            if (footer_thumbnail != null && footer_thumbnail != "none")
            {
                embed.WithFooter(footer
                     .WithIconUrl(footer_thumbnail)
                );
            }
        }

        //ACTUALLY SENDING THE EMBED!
        public async Task SendEmbed(ICommandContext context)
        {
            await context.Channel.SendMessageAsync("", false, embed.Build());
        }

        //SETTING THE COLOUR OF THE EMBED!
        private Color GetEmbedColour(EmbedColour clr)
        {
            embed = new EmbedBuilder();

            EmbedColour chosenColour = clr;

            if (chosenColour == EmbedColour.Random)
            {
                var colours = Enum.GetValues(typeof(EmbedColour)).OfType<EmbedColour>().ToArray();
                var rnd = new Random();
                var nxt = rnd.Next(1, 15);
                chosenColour = colours[nxt];
            }

            return new Color((uint)chosenColour);
        }
    }
}