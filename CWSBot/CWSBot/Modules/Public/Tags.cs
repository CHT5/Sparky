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
        private CwsContext _dctx;
        public Tags(CwsContext dctx)
        {
            _dctx = dctx;
        }

        [Command]
        public async Task TagAsync(string TagName)
        {
            //initialise the builder so we can use it later
            EmbedBuilder Builder = new EmbedBuilder();
            //retrieve the tag by name
            Tag RetrievedTag = _dctx.Tags.SingleOrDefault(x => x.Name == TagName && x.GuildId == Context.Guild.Id);
            //handle null case outside
            if (RetrievedTag != null)
            {
                //check for URLs to make sure we don't wrap them in the embed
                string[] Strings_To_Test = TagName.Split(" ");

                List<bool> DoWeHaveAUrl = new List<bool>();

                foreach (string s in Strings_To_Test)
                {
                    Uri UriResult;
                    bool IsUrl = Uri.TryCreate(s, UriKind.Absolute, out UriResult)
                        && (UriResult.Scheme == Uri.UriSchemeHttp || UriResult.Scheme == Uri.UriSchemeHttps)
                        && UriResult != null;
                    DoWeHaveAUrl.Add(IsUrl);
                }
                //if it is a URL let's send it off without the embed!
                if (DoWeHaveAUrl.Any(x => x == true))
                {
                    await ReplyAsync(string.Format("{0}\n{1}\n{2}", RetrievedTag.Name, RetrievedTag.Content, RetrievedTag.CreatorName));
                    RetrievedTag.Uses += 1;
                    _dctx.SaveChanges();
                    return;
                }
                //build our embed and send it off!
                Builder.WithTitle(RetrievedTag.Name)
                    .WithDescription(RetrievedTag.Content)
                    .WithFooter("Owner: " + RetrievedTag.CreatorName);
                await ReplyAsync("", false, Builder.Build());
                RetrievedTag.Uses += 1;
                return;
            }
            //lists to store tag names
            List<string> TagNames = new List<string>();
            List<string> TagNames_ = new List<string>();
            //add tag names to first list
            foreach (Tag _Tag in _dctx.Tags.Where(x => x.GuildId == Context.Guild.Id))
            {
                double Distance = Levenshtein.Compute(TagName, _Tag.Name);

                if (Distance / TagName.Length <= 0.42857142857)
                    TagNames.Add(_Tag.Name);
            }
            //add all the tags in the first list to the second one, just with a separator every n names.
            for (int i = 0; i < 20 && i < TagNames.Count(); i++)
            {
                string separator = ", ";
                if (i % 4 == 0 && i != 0)
                {
                    separator = "\n";
                }
                TagNames_.Add(TagNames[i] + separator);
            }
            Builder.WithTitle("Tag not found!")
                .WithDescription("Similar tags: \n" + string.Join(", ", TagNames_));
            //send result to user
            await ReplyAsync("", false, Builder.Build());
        }

        [Command("create")]
        public async Task CreateTagAsync(string TagName, [Remainder] string TagContent)
        {
            var User = (Context.User as SocketGuildUser).Roles;
            if (_dctx.Tags.Where(x => x.GuildId == Context.Guild.Id).Any(x => x.Name.ToLower() == TagName.ToLower()) || !(User.Any(x => x.Name.ToLower() == "learning")))
            {
                Emoji Denied = new Emoji("\u274c");
                await Context.Message.AddReactionAsync(Denied);
                return;
            }
            Emoji Accepted = new Emoji("\u2611");
            Tag NewTag = new Tag
            {
                Name = TagName,
                Content = TagContent,
                CreatorName = Context.User.Username,
                CreatorId = Context.User.Id,
                CreatedAt = DateTimeOffset.Now,
                Uses = 0,
                GuildId = Context.Guild.Id
            };
            _dctx.Add(NewTag);
            _dctx.SaveChanges();
            await Context.Message.AddReactionAsync(Accepted);
        }

        [Command("delete")]
        public async Task DeleteTagAsync(string TagName)
        {
            Tag TagToDelete = _dctx.Tags.SingleOrDefault(x => x.Name.ToLower() == TagName.ToLower() && x.GuildId == Context.Guild.Id);
            
            if (TagToDelete is null || TagToDelete.CreatorId != Context.User.Id)
            {
                Emoji Denied = new Emoji("\u274c");
                await Context.Message.AddReactionAsync(Denied);
                return;
            }
            Emoji Accepted = new Emoji("\u2611");
            _dctx.Remove(TagToDelete);
            _dctx.SaveChanges();
            await Context.Message.AddReactionAsync(Accepted);
        }

        [Command("edit")]
        public async Task EditTagAsync(string TagName, [Remainder] string NewContent)
        {
            Tag TagToDelete = _dctx.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == TagName.ToLower());

            if (TagToDelete is null || TagToDelete.CreatorId != Context.User.Id)
            {
                Emoji Denied = new Emoji("\u274c");
                await Context.Message.AddReactionAsync(Denied);
                return;
            }
            Emoji Accepted = new Emoji("\u2611");
            TagToDelete.Content = NewContent;
            _dctx.SaveChanges();
            await Context.Message.AddReactionAsync(Accepted);
        }

        [Command("chown")]
        public async Task ChangeOwnerAsync(string TagName, SocketGuildUser NewOwner)
        {
            Tag TagToChown = _dctx.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == TagName.ToLower());

            if (TagToChown is null || TagToChown.CreatorId != Context.User.Id)
            {
                Emoji Denied = new Emoji("\u274c");
                await Context.Message.AddReactionAsync(Denied);
                return;
            }
            Emoji Accepted = new Emoji("\u2611");
            TagToChown.CreatorId = NewOwner.Id;
            TagToChown.CreatorName = NewOwner.Username;
            _dctx.SaveChanges();
            await Context.Message.AddReactionAsync(Accepted);
        }

        [Command("info")]
        public async Task TagInfoAsync(string TagName)
        {
            Tag TagForInfo = _dctx.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == TagName.ToLower());

            if (TagForInfo is null)
            {
                Emoji Denied = new Emoji("\u274c");
                await Context.Message.AddReactionAsync(Denied);
                return;
            }
            EmbedBuilder Builder = new EmbedBuilder();
            Builder
                .AddField("Owner:", TagForInfo.CreatorName)
                .AddField("Created at:", TagForInfo.CreatedAt.ToString("ddd MMM dd HH: mm:ss"))
                .AddField("Content:", TagForInfo.Content)
                .AddField("Uses", TagForInfo.Uses);
            await ReplyAsync("", false, Builder.Build());
        }
    }
}
