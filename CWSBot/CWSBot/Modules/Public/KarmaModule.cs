using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CWSBot.Config;
using System.Diagnostics;
using CWSBot.Interaction;
using CWSBot.Misc;

namespace CWSBot.Modules.Public
{
    [Group("rep")]
    public class KarmaModule : ModuleBase
    {
        private CwsContext _dbContext;
        public KarmaModule(CwsContext dbContext)
        {
            this._dbContext = dbContext;
        }

        [Command]
        public async Task RepAsync(SocketGuildUser user)
        {
            //check that recieving user exists
            if (!_dbContext.Users.Any(x => x.UserId == user.Id))
                await AddUser(user);
            var givingDbUser = _dbContext.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
            //check that one day has passed, rep isn't being given to a bot, and that its for another user.
            if ((DateTimeOffset.Now - givingDbUser.KarmaTime).TotalDays < 1 || user.IsBot == true || user.Id == Context.User.Id)
            {
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
                return;
            }
            var recievingDbUser = _dbContext.Users.FirstOrDefault(x => x.UserId == user.Id);
            //add, save and let users know it worked. 
            if (recievingDbUser.Karma + 1 >= 30 && !user.Roles.Any(x => x.Name.ToLower() == "abyss"))
            {
                var newrole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "abyss");
                await user.AddRoleAsync(newrole);
            }
            recievingDbUser.Karma += 1;
            givingDbUser.KarmaTime = DateTimeOffset.Now;
            _dbContext.SaveChanges();
            await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
        }

        [Command("stats")]
        public async Task StatAsync(SocketGuildUser user = null)
        {
            if (user is null)
                user = Context.User as SocketGuildUser;

            var dbUser = _dbContext.Users.FirstOrDefault(x => x.UserId == user.Id);

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{user.Username}")
                .WithColor(98, 31, 193)
                .AddField("Rep:", dbUser.Karma)
                .AddField("Rep last given:", (DateTimeOffset.Now - dbUser.KarmaTime).GetHumanizedString() + " ago.");
            await ReplyAsync("", false, builder.Build());
        }

        [Command("mod")]
        public async Task ModAsync(string args = "", SocketGuildUser user = null, int args2 = 0)
        {
            if (user is null)
            {
                await ReplyAsync("usage: <rep mod reset/add/rem/rkt \\@user <int>>");
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
                return;
            }
            var dbUser = _dbContext.Users.FirstOrDefault(x => x.UserId == user.Id);
            switch (args)
            {
                case "reset":
                    dbUser.Karma = 0;
                    break;
                case "add":
                    dbUser.Karma += args2;
                    if (dbUser.Karma + args2 >= 30)
                    {
                        var newrole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "abyss");
                        await user.AddRoleAsync(newrole);
                    }
                    break;
                case "rem":
                    dbUser.Karma -= args2;
                    if (dbUser.Karma - args2 < 30)
                    {
                        var newrole = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToLower() == "abyss");
                        await user.RemoveRoleAsync(newrole);
                    }
                    break;
                case "rkt":
                    dbUser.KarmaTime = DateTimeOffset.Now.AddDays(-1);
                    break;
                default:
                    await ReplyAsync("usage: <rep mod reset/add/rem/rkt \\@user <int>>");
                    await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
                    break;
            }
            _dbContext.SaveChanges();
            await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
        }
        //need a global solution to ensure there are no future conflicts.
        private Task AddUser(SocketGuildUser user)
        {
            using (var dctx = new CwsContext())
            {
                User DbUser = dctx.Users.SingleOrDefault(x => x.UserId == user.Id);

                if (DbUser is null)
                {
                    User NewDbUser = new User
                    {
                        UserId = user.Id,
                        Karma = 0,
                        WarningCount = 0,
                        MessageCount = 1,
                        Tokens = 0,
                        KarmaTime = DateTimeOffset.Now.AddDays(-1),
                        Username = user.Username
                    };

                    dctx.Add(NewDbUser);
                    dctx.SaveChanges();
                    return Task.CompletedTask;
                }

                DbUser.MessageCount++;
                dctx.SaveChanges();
                return Task.CompletedTask;
            }
        }
    }
}
