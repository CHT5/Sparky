using Discord.Commands;
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

        //This is a JS code to convert (hopefully I'll do it soon, yours sincerely Maxi)

        /*
        var fighters = message.mentions.users.map(m => m.username); //Gets all the mentioned users

        // Return message in case noone was mentioned
        if (fighters.length == 0) return message.channel.send("**WHAT?!** I scheduled the fite but **NOBODY CAME?!** Mention some users to call them out first!");

        // Return message in case only on person was mentioned 
        if (fighters.length == 1) return message.channel.send("**It's no fun alone!** Mention more people, **COWARD!**");

        // Copies the fighters into a new array excluding duplicates
        var fightersProcessed = fighters.filter(function(item, index, inputArray) {
            return inputArray.indexOf(item) == index;
        });

        var winner = Math.floor(Math.random() * fighters.length); // Determine winner early on (pick random)

        // Insert crossed swords between mentions
        var i, j;
        for (i = 0, j = 1; i<fighters.length; i++) {
            fightersProcessed.splice([j], 0, ":crossed_swords:");
            j += 2;
        }

        fightersProcessed.splice(-1, j); //Remove one last crossed swords sign

        if (fighters.length >= 5) {
            message.channel.send(`**${fightersProcessed.join(" ")}**\n\nIt's been a tough one, but **${fighters[winner]}** managed to avoid the battle and waited until there was only one opponent left. **${fighters[winner]}** successfully knocked that last opponent out and won!\n\n:crown:\n**${fighters[winner]}**`);
        }
        else {
            message.channel.send(`**${fightersProcessed.join(" ")}**\n\nThe winner is: **${fighters[winner]}**\n\n:crown:\n**${fighters[winner]}**`);
        }
        console.log("The following people decided to fight: " + fighters + "... and " + fighters[winner] + " won!");
    });
    */
    }
}
