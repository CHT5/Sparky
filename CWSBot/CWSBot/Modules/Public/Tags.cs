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
            EmbedBuilder builder = new EmbedBuilder();
            //retrieve the tag by name
            Tag recievedTag = _dctx.Tags.SingleOrDefault(t => t.Name == TagName && t.GuildId == Context.Guild.Id);
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
                    _dctx.SaveChanges();

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
            foreach (Tag tag in _dctx.Tags.Where(x => x.GuildId == Context.Guild.Id))
            {
                double Distance = Levenshtein.Compute(TagName, tag.Name);

                if (Distance / TagName.Length <= 0.42857142857)
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
        public async Task CreateTagAsync(string TagName, [Remainder] string TagContent)
        {
            var User = (Context.User as SocketGuildUser).Roles;
            if (_dctx.Tags.Where(x => x.GuildId == Context.Guild.Id).Any(x => x.Name.ToLower() == TagName.ToLower()))
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
