using CWSBot.Interaction;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Security.Cryptography.RandomNumberGenerator;

namespace CWSBot.Modules.Public
{
    public class CasinoModule : ModuleBase<SocketCommandContext>
    {
        private CwsContext _dctx;

        public CasinoModule(CwsContext dctx)
        {
            _dctx = dctx;
        }

        EasyEmbed embed_registration = new EasyEmbed();

        [Command("gamble", RunMode = RunMode.Async)]
        [Alias("gamba")]
        [Remarks("Rolls two dice, based on the number you roll you will either win or lose tokens.")]
        public async Task Gamble(double stakes)
        {
            var userMoney = _dctx.Users.SingleOrDefault(x => x.UserId == Context.User.Id);
            if (userMoney.Tokens < stakes)
            {
                await ReplyAsync($"(GambleDragon) :dragon_face:: {Context.User.Mention}, you're missing {stakes - userMoney.Tokens} tokens to do that!\n" +
                    $"(Your current tokens: {userMoney.Tokens})!");
                return;
            }
            if (stakes < 1 || stakes > 50)
            {
                await ReplyAsync($"(GambleDragon) :dragon_face:: I'm sorry, we only take wagers between 1-50.");
                return;
            }
            
            Random rnd = new Random();
            double dice1Roll = rnd.Next(1, 7);
            double dice2Roll = rnd.Next(1, 7);
            double pointResult = 0;
            string rollResultText = $"(GambleDragon) :dragon_face:: {Context.User.Mention}, Your roll consisting of a __{dice1Roll}__ and a __{dice2Roll}__, earned points for: \n";
            if (dice1Roll == dice2Roll)
            {
                pointResult += (int)Math.Ceiling(stakes * 0.5);
                rollResultText += $":diamonds: rolling a double ({dice1Roll}). *+{(int)Math.Ceiling(stakes * 0.5)} points!*\n";
            }
            if (dice1Roll == 6 || dice2Roll == 6)
            {
                if(dice1Roll == 6 && dice2Roll == 6)
                {
                    pointResult += (int)Math.Ceiling(stakes * 2.4);
                    rollResultText += $":hearts: rolling two 6's. *+{(int)Math.Ceiling(stakes * 2.4)} points!*\n";
                }
                else
                {
                    pointResult += (int)Math.Ceiling(stakes * 1.2);
                    rollResultText += $":hearts: rolling a 6. *+{(int)Math.Ceiling(stakes * 1.2)} points!*\n";
                }
            }
            if((dice1Roll+dice2Roll)%4 == 0)
            {
                pointResult += (int)Math.Ceiling(stakes * ((3 * ((dice1Roll+dice2Roll)/4))/10));
                rollResultText += $":spades: the sum of your roll was a multiple of 4. *+{(int)Math.Ceiling(stakes * (3 * ((dice1Roll + dice2Roll) / 4) / 10))} point(s)!*\n";
            }
            if ((dice1Roll + dice2Roll) % 3 == 0)
            {
                pointResult += (int)Math.Ceiling(stakes * ((3 * ((dice1Roll + dice2Roll) / 3)) / 10));
                rollResultText += $":clubs: the sum of your roll was a multiple of 3. *+{(int)Math.Ceiling(stakes * (3 * ((dice1Roll + dice2Roll) / 3) / 10))} point(s)!*\n";
            }

            stakes *= -1;

            if (pointResult == 0)
            {
                rollResultText = $"(GambleDragon) :dragon_face:: {Context.User.Mention}, Your roll consisting of a __{dice1Roll}__ and a __{dice2Roll}__, earned you no points.\n" +
                    $"*as such, I'll be taking your money.* ***({stakes} tokens)***";
            }
            else
            {
                if ((stakes + pointResult) >= 0)
                {
                    rollResultText += $":game_die: this comes down to a total of {pointResult} points!\n\n" +
                        $"***Stakes: {stakes}, Points: {pointResult}, Actual profit: {stakes + pointResult} :moneybag:***\n";
                }
                else
                {
                    rollResultText += $":game_die: this comes down to a total of {pointResult} points!\n\n" +
                        $"***Stakes: {stakes}, Points: {pointResult}, Actual loss: {stakes + pointResult} :money_with_wings:***\n";
                }

            }
            int profitResult = Convert.ToInt32(stakes + pointResult);

            userMoney.Tokens -= Convert.ToUInt32(profitResult);
            _dctx.SaveChanges();
            await ReplyAsync(rollResultText);
        }
    }
}
