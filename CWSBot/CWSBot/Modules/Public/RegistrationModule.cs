using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.InteractiveCommands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;

namespace CWSBot.Modules.Public
{
    public class RegistrationModule : ModuleBase<SocketCommandContext>
    {

        EasyEmbed embed_registration = new EasyEmbed();
        public InteractiveService Interactive { get; set; }

        [Command("register", RunMode = RunMode.Async)]
        public async Task Register()
        {

            //SET GUILD, USER AND GENERAL CHANNEL
            var guild = Context.Client.GetGuild(351284764352839690);
            var user = guild.GetUser(Context.User.Id);
            var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "general") as SocketTextChannel;

            var roles = new List<SocketRole>();
            var role = guild.Roles.FirstOrDefault(has => has.Name.ToUpper() == "LiteBox Member".ToUpper()) as SocketRole;
            roles.Add(role);

            var roleName = "LiteBox Member";
            var userRoles = user.Roles.FirstOrDefault(has => has.Name.ToUpper() == roleName.ToUpper());


            if (userRoles != null)
            {
                await ReplyAsync($"{user.Mention}, you have already registered!\n" +
                    $"*If you'd like to make changes, please contact a staff member.*");
                return;
            }

            //SETUP FOR EMBED IMAGES AND COLOUR
            //CUSTOMIZE THE URL'S FOR THE PICTURES USED IN THE EMBEDS
            var icon_Information = "http://icons.iconarchive.com/icons/visualpharm/must-have/256/Information-icon.png";
            var icon_Questionmark = "http://images.clipartpanda.com/question-mark-icon-yTo7rX4TE.png";
            var icon_Support = "http://cdn2.hubspot.net/hub/423318/file-2015757038-png/Graphics/Benefits/vControl-Icon-Large_Helpdesk.png";
            var embed_Colour = EasyEmbed.EmbedColour.LightAqua; //SEE THE "EasyEmbed.cs" CLASS FOR THE LIST OF AVAILABLE COLOURS.

            //VARS TO BE STORED IN THE DATABASE
            var register_Name = Context.User.Username;
            var register_Age = 0;
            var register_Gamer = "";
            var register_Programmer = "";
            var register_DigialArtist = "";
            var register_SoundArtist = "";
            TimeSpan questionTimeout = TimeSpan.FromSeconds(50);

            //INTRO + QUESTION 1: ASKING FOR AGE
            embed_registration.createFooterEmbed(embed_Colour, "Thank you for taking the time to register!", "All questions will timeout after 1 minute. \n" +
                "You don't need to use the prefix `!` to answer.\n" +
                "\n" +
                "**Q __1__/5**: How old are you?", icon_Information, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

            await embed_registration.sendEmbed(Context);

            var response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel,null);


            while (register_Age == 0)
            {
                //STORE THE AGE
                if (Int32.TryParse(response.Content, out register_Age))
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"{Context.User.Username}, **Q __2__/5**: ", "Do you like to play video games by chance?\n" +
                        "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                }
                //TRY AGAIN IF NOT A VALID AGE
                else
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"**Q __1__/5**: Whoops! something went wrong {Context.User.Username}!", "You didn't enter a valid number!\n" +
                        "\n" +
                        "Please re-enter your age!", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                    response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
                }
            }

            //QUESTION 2: ASKING ABOUT VIDYA GAMES
            response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
            register_Gamer = response.Content;


            while (register_Gamer != "yes" && register_Gamer != "no")
            {
                if (register_Gamer != "yes" && register_Gamer != "no")
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"**Q __2__/5**: Whoops!", "Something went wrong there  {Context.User.Username}\n" +
                        "\n" +
                        "Please tell me wether you do or don't like playing video games again...\n" +
                        "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                }
                //STORE THE GAMER STATUS
                response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
                register_Gamer = response.Content;
            }


            //QUESTION 3: ASKING ABOUT PROGRAMMING
            embed_registration.createFooterEmbed(embed_Colour, $"{Context.User.Username}, **Q __4__/5**: ", "Are you by chance also interested in programming?\n" +
                "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

            await embed_registration.sendEmbed(Context);
            response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
            register_Programmer = response.Content;


            while (register_Programmer != "yes" && register_Programmer != "no")
            {
                if (register_Programmer != "yes" && register_Programmer != "no")
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"**Q __3__/5**: Whoops!", "Something went wrong there...\n" +
                        "\n" +
                        "Please tell me wether you are or aren't interested in programming again...\n" +
                        "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                }
                //STORE THE GAMER STATUS
                response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
                register_Programmer = response.Content;
            }

            //QUESTION 4: ASKING ABOUT DIGITAL ART
            embed_registration.createFooterEmbed(embed_Colour, $"{Context.User.Username}, **Q __4__/5**: ", "And what about creating digital art?\n" +
                "Examples: 2D/3D art, 3D Modelling, etc.\n" +
                "\n" +
                "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

            await embed_registration.sendEmbed(Context);
            response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
            register_DigialArtist = response.Content;


            while (register_DigialArtist != "yes" && register_DigialArtist != "no")
            {
                if (register_DigialArtist != "yes" && register_DigialArtist != "no")
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"**Q __4__/5**: Whoops!", "Something went wrong there...\n" +
                        "\n" +
                        "Please tell me wether you are or aren't interested in digital art again...\n" +
                        "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                }
                //STORE THE GAMER STATUS
                response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
                register_Programmer = response.Content;
            }

            //QUESTION 5: ASKING ABOUT SOUND PRODUCER
            embed_registration.createFooterEmbed(embed_Colour, $"{Context.User.Username}, **Q __5__/5**: ", "Are you by chance also into the composing/producing of sound/sfx's?\n" +
                "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

            await embed_registration.sendEmbed(Context);
            response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
            register_SoundArtist = response.Content;


            while (register_SoundArtist != "yes" && register_SoundArtist != "no")
            {
                if (register_SoundArtist != "yes" && register_SoundArtist != "no")
                {
                    embed_registration.createFooterEmbed(embed_Colour, $"**Q __5__/5**: Whoops!", "Something went wrong there...\n" +
                        "\n" +
                        "Please tell me wether you are or aren't into the composing/producing of sound/sfx's again...\n" +
                        "***Answer:*** *yes/no*", icon_Questionmark, $"For issues with, or questions about the bot, please refer to @LunarLite#7766", icon_Support);

                    await embed_registration.sendEmbed(Context);
                }
                //STORE THE GAMER STATUS
                response = await Interactive.WaitForMessage(Context.Message.Author, Context.Channel);
                register_Programmer = response.Content;
            }
            await ReplyAsync($"{Context.User.Mention}, that finalizes your regstration, have fun on the server!");



            //IS USER A GAMER?
            if (register_Gamer == "yes")
            {
                role = guild.Roles.FirstOrDefault(has => has.Name.ToUpper() == "Gamer".ToUpper()) as SocketRole;
                roles.Add(role);
            }
            //IS USER A PROGRAMMER?
            if (register_Programmer== "yes")
            {
                role = guild.Roles.FirstOrDefault(has => has.Name.ToUpper() == "Programmer".ToUpper()) as SocketRole;
                roles.Add(role);
            }
            //IS USER A ARTIST?
            if (register_DigialArtist == "yes")
            {
                role = guild.Roles.FirstOrDefault(has => has.Name.ToUpper() == "Digital Artist".ToUpper()) as SocketRole;
                roles.Add(role);
            }
            //IS USER A AUDIO ENGINEER
            if (register_SoundArtist == "yes")
            {
                role = guild.Roles.FirstOrDefault(has => has.Name.ToUpper() == "Audio Engineer".ToUpper()) as SocketRole;
                roles.Add(role);
            }
            //ADD ROLES TO USER
            await user.AddRolesAsync(roles);

            // welcome user
            await channel.SendMessageAsync($"{user.Mention}, welcome to the {guild.Name} server!!");
            // mod-log it.
            channel = guild.Channels.FirstOrDefault(xc => xc.Name == "user-logs") as SocketTextChannel;
            await channel.SendMessageAsync($"```ini\n [{user}] succesfully registered.```");

            Database.EnterUser(Context.User, register_Age);
        }
    }
}