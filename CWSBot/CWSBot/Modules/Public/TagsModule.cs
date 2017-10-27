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
    public class TagsModule : ModuleBase
    {
        private CwsContext _dbContext;

        private Color _embedColor;

        protected override void BeforeExecute(CommandInfo command)
        {
            if (Context.Channel is IGuildChannel)
            {
                _embedColor = (Context.User as SocketGuildUser).Roles.OrderByDescending(x => x.Position)
                                                                     .Where(x => x.Color.RawValue != Color.Default.RawValue)
                                                                     .FirstOrDefault()?.Color ?? Color.Purple;
            }
            else _embedColor = Color.Purple;
        }

        public TagsModule(CwsContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Command]
        public async Task TagAsync(string name)
        {
            //initialise the builder so we can use it later
            var builder = new EmbedBuilder();

            builder.Color = _embedColor;
            //retrieve the tag by name
            var recievedTag = _dbContext.Tags.SingleOrDefault(t => t.Name.ToLower() == name.ToLower() && t.GuildId == Context.Guild.Id);
            //handle null case outside
            if (recievedTag != null)
            {
                //check for URLs to make sure we don't wrap them in the embed
                var strArray = recievedTag.Content.Split(" ");

                var containsUrl = strArray.Any(s => Uri.TryCreate(s, UriKind.Absolute, out var UriResult)
                        && (UriResult.Scheme == Uri.UriSchemeHttp || UriResult.Scheme == Uri.UriSchemeHttps)
                        && UriResult != null);
                //if it is a URL let's send it off without the embed!
                if (containsUrl)
                {
                    // check for images
                    var image = strArray.FirstOrDefault(x => Uri.TryCreate(x, UriKind.Absolute, out var res) && res.PointsToImage());

                    // if there's an image then put it as embed image
                    if (image != null)
                    {
                        var description = string.Join(" ", strArray.Where(x => x != image));

                        builder.WithTitle(recievedTag.Name)
                               .WithFooter("Owner: " + recievedTag.CreatorName);

                        if (!string.IsNullOrWhiteSpace(description))
                            builder.Description = description;

                        builder.ImageUrl = image;

                        await ReplyAsync("", embed: builder.Build());
                    }
                    else
                    {
                        await ReplyAsync(string.Format("{0}: \n{1}", recievedTag.Name, recievedTag.Content));
                    }

                    recievedTag.Uses += 1;
                    _dbContext.SaveChanges();

                    return;
                }
                //build our embed and send it off!
                builder.WithTitle(recievedTag.Name)
                    .WithDescription(recievedTag.Content)
                    .WithFooter("Owner: " + recievedTag.CreatorName);

                await ReplyAsync("", embed: builder.Build());
                recievedTag.Uses += 1;
                return;
            }

            //add tag names to first list
            var tags = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id);

            var tagNames = tags.AsEnumerable()
                               .Select(x => (Distance: Levenshtein.Compute(name, x.Name) / x.Name.Length, Tag: x))
                               .Where(t => t.Distance <= 0.42857142857)
                               .Select(x => x.Tag); 

            if (tagNames.Count() == 0)
            {
                builder.WithTitle("No matching tags found")
                    .WithDescription("There were no tags found matching that description. Use <tag list> to find some!");
                await ReplyAsync("", false, builder.Build());

                return;
            }

            string response = string.Join("", tagNames.Select((x, i) => $"{x.Name}{(i % 3 == 0 && i != 0 ? "\n" : ", ")}"));
            response = response.Remove(response.Length - 2);

            builder.WithTitle("Tag not found!")
                .WithDescription("Similar tags: \n" + response);
            //send result to user
            await ReplyAsync("", embed: builder.Build());
        }

        [Command("mod"), RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task ModTagAsync(string args, string Name, [Remainder] string content = null)
        {
            switch (args)
            {
                case "delete":
                    Tag tag = _dbContext.Tags.SingleOrDefault(x => x.Name.ToLower() == Name.ToLower() && x.GuildId == Context.Guild.Id);

                    if (tag is null)
                        await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
                    else
                    {
                        _dbContext.Remove(tag);
                        _dbContext.SaveChanges();

                        await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
                    }
                    break;
                case "edit":
                    Tag tag_ = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).SingleOrDefault(x => x.Name.ToLower() == Name.ToLower());

                    if (tag_ is null)
                        await Context.Message.AddReactionAsync(new Emoji("\u274c")); // Denied emoji
                    else
                    {
                        tag_.Content = content;
                        _dbContext.SaveChanges();

                        await Context.Message.AddReactionAsync(new Emoji("\u2611")); // Accepted emoji
                    }
                    break;
                default:
                    await ReplyAsync("usage: <tag mod delete/edit>");
                    break;
            }
        }
        [Command("create")]
        public async Task CreateTagAsync(string Name, [Remainder] string Content)
        {
            if (_dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id).Any(x => x.Name.ToLower() == Name.ToLower()) || (Context.User as SocketGuildUser).Roles.Any(x => x.Name.ToLower() == "learning"))
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
                                .WithColor(98, 31, 193)
                                .AddField("Owner:", tag.CreatorName)
                                .AddField("Created at:", tag.CreatedAt.ToString("ddd MMM dd HH: mm:ss"))
                                .AddField("Content:", tag.Content)
                                .AddField("Uses", tag.Uses);

                await ReplyAsync("", false, builder.Build());
            }
        }

        [Command("list")]
        public async Task ListTagsAsync(string Name = "")
        {
            EmbedBuilder builder = new EmbedBuilder();

            var storedTags = _dbContext.Tags.Where(x => x.GuildId == Context.Guild.Id);
            string descString = string.Join(", ", storedTags.Select(x => x.Name));

            if (descString.Length > 2000)
                descString = descString.Substring(0, 2000);

            builder.WithTitle($"Server Tags for {Context.Guild.Name}")
                .WithColor(98, 31, 193)
                .WithDescription(descString);
            await ReplyAsync("", false, builder.Build());
        }
    }
}
