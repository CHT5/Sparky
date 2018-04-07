using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Sparky.Data;

namespace Sparky.Objects
{
    public static class AuditLogHelper
    {
        public static async Task<string> GenerateMessageAsync(this ModLogEntry entry, DiscordClient client, IConfiguration config)
        {
            var responsibleText = string.Empty;
            var responsibleUser = await client.GetUserAsync(entry.ResponsibleModId.Value);
            var guild = await client.GetGuildAsync(entry.GuildId);
            var roleName = string.Empty;
            
            if (entry.RoleAdded.HasValue)
            {
                var role = guild.GetRole(entry.RoleAdded.Value);
            }

            if (responsibleUser == null)
                throw new Exception($"Could not resolve user of Id {entry.ResponsibleModId ?? 0}");

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(Formatter.Bold("Responsible staff member:"));
            if (entry.ResponsibleModId != client.CurrentUser.Id)
                stringBuilder.AppendFormat(" {0}#{1}", responsibleUser.Username, responsibleUser.Discriminator);
            else
                stringBuilder.AppendFormat(" {0}#{1}", responsibleUser.Username, responsibleUser.Discriminator);
                //stringBuilder.Append(Formatter.Italic($"_Responsible moderator, please type `{config["prefix"]}sign {entry.CaseNumber}`_"));

            var logEntry = $"**{entry.Action.Humanize()}{(!string.IsNullOrEmpty(roleName) ? $": {roleName}" : string.Empty)}** | Case {entry.CaseNumber}\n" +
                           $"**User**: {entry.TargetUsername}#{entry.TargetDiscriminator} ({entry.TargetUserId}) (<@!{entry.TargetUserId}>)\n" +
                           $"**Reason**: {entry.Reason ?? $"_Responsible moderator, please type `{config["prefix"]}reason {entry.CaseNumber} <reason>`_"}\n" +
                           stringBuilder.ToString();
            return logEntry;
        }

        public static async Task SendLogAsync(this ModLogEntry entry, DiscordGuild guild, IConfiguration config, DiscordClient client)
        {
            var logChannel = guild.Channels.FirstOrDefault(x => x.Name == config["channels:mod_log_channel_name"]);
            if (logChannel == null)
                throw new Exception("No mod log channel could be found");

            var logMessage = await entry.GenerateMessageAsync(client, config);
            
            var message = await logChannel.SendMessageAsync(logMessage);

            entry.Modify(x => x.ModLogMessageId = message.Id);
        }
    }
}