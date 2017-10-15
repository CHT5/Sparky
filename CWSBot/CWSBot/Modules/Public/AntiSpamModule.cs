using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CWSBot.Modules.Public
{
    public class AntiSpamModule : ModuleBase<SocketCommandContext> // <-- That part after : is probly wrong :/
    {
		// The code from the Old Sparky C# aka antispam itself...
        public async Task HandleCommand(SocketMessage parameterMessage)
        {
            var message = parameterMessage as SocketUserMessage;

            if (message == null) return;

            int argPos = 0;

            var context = new CommandContext(client, message);

            var currentDateTime = DateTime.Now;

            var messagesToRemove = messageList.Where(input => input.Item3.AddSeconds(15) <= currentDateTime).ToList();

            messageList.RemoveAll(input => input.Item3.AddSeconds(15) <= currentDateTime);

            if (parameterMessage.Channel.Id != 280055225069469696)
            {
                messageList.Add(new Tuple<ulong, string, DateTime>(parameterMessage.Author.Id, parameterMessage.ToString(), DateTime.Now));

                var repeatedMessages = messageList.Where(input => input.Item1 == parameterMessage.Author.Id && input.Item2.ToUpper() == parameterMessage.ToString().ToUpper());

                if (repeatedMessages.Count() >= 9) // enthousiastic typing ~ 7 messages / 20 seconds. Should make 9 on 15 very reasonable.
                {
                    var earliestEntry = repeatedMessages.OrderBy(input => input.Item3).FirstOrDefault().Item3;

                    var latestEntry = repeatedMessages.OrderByDescending(input => input.Item3).FirstOrDefault().Item3;

                    var timeSpan = latestEntry - earliestEntry;

                    if (timeSpan.TotalSeconds <= 15)
                    {
                        await context.Guild.AddBanAsync(parameterMessage.Author);

                        var channel = client.GetChannel(285528212027473920) as SocketTextChannel;

                        await channel.SendMessageAsync(string.Format("I have banned user {0} for spamming!", parameterMessage.Author));
                    }
                }
            }

            if (!(message.HasMentionPrefix(client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos))) return;
            
            var result = await commands.ExecuteAsync(context, argPos, map);
            
            if (!result.IsSuccess)
                await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");

        }
	}
}