using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Security.Cryptography.RandomNumberGenerator;

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

            var FilteredUsers = Users.GroupBy(x => x.Id).Select(x => x.FirstOrDefault());

            Random r = new Random();
            SocketGuildUser winner = FilteredUsers.ElementAt(r.Next(0, (FilteredUsers.Count() - 1)));

            string competitorList = string.Join(":crossed_swords:", FilteredUsers.Select(x => x.Username));
            string fightMessage = string.Format("\nIt's been a tough one, but **{0}** managed to avoid the battle and waited until there was only one opponent left!", winner.Username);
            string lastMessage = string.Format("**{0}** managed to knock the last opponent out and won the battle!\n\n:crown:\n{1}", winner.Username, winner.Mention);

            await ReplyAsync(competitorList + "\n" + fightMessage);
            await Task.Delay(1000);
            await ReplyAsync(lastMessage);
        }
    }
}
