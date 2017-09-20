using CWSBot.Interaction;
using CWSBot.Misc;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CWSBot.Modules.Public
{
    [Group("tag"), Alias("tags")]
    public class Tags : ModuleBase
    {
        private CwsContext _dbContext;

        public Tags(CwsContext DatabaseContext)
        {
            _dbContext = DatabaseContext;
        }

        [Command]
        public async Task TagAsync(string Name)
        {
            //initialise the builder so we can use it later
            EmbedBuilder builder = new EmbedBuilder();
            //retrieve the tag by name
            Tag recievedTag = _dbContext.Tags.SingleOrDefault(t => t.Name == Name && t.GuildId == Context.Guild.Id);
            //handle null case outside
            if (recievedTag != null)
            {
                //check for URLs to make sure we don't wrap them in the embed
                string[] strArray = recievedTag.Content.Split(" ");

                bool containsUrl = strArray.Any(s => Uri.TryCreate(s, UriKind.Absolute, out Uri UriResult)
                        && (UriResult.Scheme == Uri.UriSchemeHttp || UriResult.Scheme == Uri.UriSchemeHttps)
                        && UriResult != null);
                //if it is a URL let's send it off without the embed!
                if (containsUrl)
                {
                    await ReplyAsync(string.Format("Tag: {0}\nContent: {1}\nOwner: {2}", recievedTag.Name, recievedTag.Content, recievedTag.CreatorName));

                    recievedTag.Uses += 1;
                    _dbContext.SaveChanges();

                    return;
                }
                //build our embed and send it off!
                builder.WithTitle(recievedTag.Name)
                    .WithDescription(recievedTag.Content)
                    .WithFooter("Owner: " + recievedTag.CreatorName);

                await ReplyAsync("", false, builder.Build());
                recievedTag.Uses += 1;
                return;
            }
            //lists to store tag names
            List<string> tagNames = new List<string>();
            List<string> tagNames_ = new List<string>();
            //add tag names to first list
            foreach (Tag tag in _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id))
            {
                double Distance = Levenshtein.Compute(Name, tag.Name);

                if (Distance / Name.Length <= 0.42857142857)
                    tagNames.Add(tag.Name);
            }
            //add all the tags in the first list to the second one, just with a separator every n names.
            for (int i = 0; i < 20 && i < tagNames.Count(); i++)
            {
                var separator = i % 4 == 0 && i != 0 ? "\n" : ", ";

                tagNames_.Add(tagNames[i] + separator);
            }

            string response = string.Join(", ", tagNames_);
            response = response.Remove(response.Length - 2);

            builder.WithTitle("Tag not found!")
                .WithDescription("Similar tags: \n" + response);
            //send result to user
            await ReplyAsync("", false, builder.Build());
        }

        [Command("create")]
        public async Task CreateTagAsync(string Name, [Remainder] string Content)
        {
            if (_dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).Any(x => x.Name.ToLower() == Name.ToLower()))
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
            else
            {
                _dbContext.Add(new Tag
                {
                    Name = Name,
                    Content = Content,
                    CreatorName = Context.User.Username,
                    CreatorId = Context.User.Id,
                    CreatedAt = DateTimeOffset.Now,
                    Uses = 0,
                    GuildId = Context.Guild.Id
                });

                _dbContext.SaveChanges();
                await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
            }
        }

        [Command("delete")]
        public async Task DeleteTagAsync(string Name)
        {
            Tag tag = _dbContext.Tags.SingleOrDefault(x => x.Name.ToLower() == Name.ToLower() && x.GuildId == Context.Guild.Id);

            if (tag is null || tag.CreatorId != Context.User.Id)
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
            else
            {
                _dbContext.Remove(tag);
                _dbContext.SaveChanges();

                await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
            }
        }

        [Command("edit")]
        public async Task EditTagAsync(string Name, [Remainder] string Content)
        {
            Tag tag = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == Name.ToLower());

            if (tag is null || tag.CreatorId != Context.User.Id)
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
            else
            {
                tag.Content = Content;
                _dbContext.SaveChanges();

                await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
            }
        }

        [Command("chown")]
        public async Task ChangeOwnerAsync(string Name, SocketGuildUser NewOwner)
        {
            Tag tag = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == Name.ToLower());

            if (tag is null || tag.CreatorId != Context.User.Id)
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
            else
            {
                tag.CreatorId = NewOwner.Id;
                tag.CreatorName = NewOwner.Username;

                _dbContext.SaveChanges();

                await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
            }
        }

        [Command("info")]
        public async Task TagInfoAsync(string Name)
        {
            Tag tag = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == Name.ToLower());

            if (tag is null)
                await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
            else
            {
                var builder = new EmbedBuilder()
                                .AddField("Owner:", tag.CreatorName)
                                .AddField("Created at:", tag.CreatedAt.ToString("ddd MMM dd HH: mm:ss"))
                                .AddField("Content:", tag.Content)
                                .AddField("Uses", tag.Uses);

                await ReplyAsync("", false, builder.Build());
            }
        }
    }
}
