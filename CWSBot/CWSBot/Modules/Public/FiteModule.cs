using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CWSBot.Modules.Public
{
    public class FiteModule : ModuleBase<SocketCommandContext>
    {
        // Arena fight minigame
        [Command("fite", RunMode = RunMode.Async)]
        [Alias("fight")]
        [Remarks("Initiates a fight between mentioned users and picks a random winner.")]
        public async Task FiteAsync(params SocketGuildUser[] Users)
        {
            if (Users.Count() == 0)
            {
                await ReplyAsync("**WHAT?!** I scheduled the fite but **NOBODY CAME?!** Mention some users to call them out first!");
                return;
            }

            if (Users.Count() == 1)
            {
                await ReplyAsync("**It's no fun alone!** Mention more people, **COWARD!**");
                return;
            }
            
			var fightMessageContext0 = "managed to avoid the battle and waited until there was only one opponent left!";
            var fightMessageContext1 = "the last opponent out";
            
			if (Users.Count() == 2)
            {
                fightMessageContext0 = "was the better fighter and tired out their opponent!";
                fightMessageContext1 = "out their competitor";
            }

            var FilteredUsers = Users.GroupBy(x => x.Id).Select(x => x.FirstOrDefault());

            if (FilteredUsers.Count() <= 1)
            {
                await ReplyAsync("One doesn't simply fite oneself, silly!");
                return;
            }

            var rand = new Random();

            var winner = FilteredUsers.ElementAt(rand.Next(0, (FilteredUsers.Count())));

            var competitorList = string.Join(" :crossed_swords: ", FilteredUsers.Select(x => x.Username));
            var fightMessage = $"\nIt's been a tough one, but **{winner.Username}** {fightMessageContext0}";
            var lastMessage = $"**{winner.Username}** managed to knock {fightMessageContext1} and won the battle!\n\n:crown:\n**{winner.Username}**";

            await ReplyAsync(competitorList + "\n" + fightMessage);
            await Task.Delay(1000);
            await ReplyAsync(lastMessage);
        }
    }
}
