using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CWSBot.Config;
using System.IO;
using CWSBot;
using System.Linq;

namespace CWSBot
{
    class Program
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider _provider;

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            EnsureBotConfigExists(); // Ensure that the bot configuration json file has been created.

            client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
            });
            commands = new CommandService();

            client.Log += Log;
            commands.Log += Log;
            //client.UserLeft += UserLeft;
            //client.UserJoined += UserJoined;
            string token = BotConfig.Load().Token;

            await InstallCommands();

            var serviceProvider = ConfigureServices();
            _provider = serviceProvider;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        /*private Task UserLeft(SocketGuildUser arg)
        {
            //SET GUILD, USER AND GENERAL CHANNEL
            var guild = client.GetGuild(351284764352839690);
            var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "user-logs") as SocketTextChannel;
            channel.SendMessageAsync($"```ini\n [{arg}] left the server.```");

            Database.RemoveUser(arg);
            return Task.CompletedTask;
        }

        private Task UserJoined(SocketGuildUser arg)
        {
            //SET GUILD, USER AND GENERAL CHANNEL
            var guild = client.GetGuild(351284764352839690);
            var channel = guild.Channels.FirstOrDefault(xc => xc.Name == "user-logs") as SocketTextChannel;
            channel.SendMessageAsync($"```ini\n [{arg}] joined the server.```");

            Database.RemoveUser(arg);
            return Task.CompletedTask;
        }*/

        public async Task AccountJoined(SocketGuildUser user)
        {
            SocketTextChannel welcomeChannelPublic = user.Guild.TextChannels.FirstOrDefault(x => x.Name == "offtopic_discussions");
            SocketTextChannel welcomeChannelMod = user.Guild.TextChannels.FirstOrDefault(x => x.Name == "cwsbot_reports");
            SocketRole learningRole = user.Guild.Roles.FirstOrDefault(x => x.Name == "Learning");
            SocketRole botsRole = user.Guild.Roles.FirstOrDefault(x => x.Name == "Bots");

            string userDetails = "```ini\n ";
            string roleAddedText = "```ini\n [";
            
            userDetails += string.Format("[{0}]{1}", user.Username, user.IsBot ? " bot has" : string.Empty);
            roleAddedText += user.IsBot ? botsRole.Name : learningRole.Name;
            await user.AddRoleAsync(user.IsBot ? botsRole : learningRole);

            string welcomeText = userDetails + " joined the server!```";
            roleAddedText += $"] has been assigned to [{user.Username}] !```";

            await welcomeChannelMod.SendMessageAsync(welcomeText);
            await welcomeChannelPublic.SendMessageAsync(welcomeText);
            await welcomeChannelMod.SendMessageAsync(roleAddedText);
        }

        public async Task AccountLeft(SocketGuildUser user)
        {
            SocketTextChannel goodbyeChannelMod = user.Guild.TextChannels.FirstOrDefault(x => x.Name == "cwsbot_reports");

            await goodbyeChannelMod.SendMessageAsync($"```ini\n [{user.Username}] left the server!```");
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(client)
            //.AddSingleton<AudioService>() remove Slashes if you have audio
                .AddSingleton(new CommandService(new CommandServiceConfig
                {
                    CaseSensitiveCommands = false
                }));
            var provider = new DefaultServiceProviderFactory().CreateServiceProvider(services);
            //provider.GetService<AudioService>(); Remove slashes if you have audio
            return provider;
        }

        public async Task InstallCommands()
        {
            client.MessageReceived += HandleCommand;
            client.UserJoined += AccountJoined;
            client.UserLeft += AccountLeft;
            client.Ready += OnConnected;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task OnConnected()
        {
            //Set game status.
            await client.SetGameAsync(BotConfig.Load().Prefix + "help with help");
        }
        
        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            if (message == null) return;

            int argPos = 1;

            if (!(message.HasStringPrefix(BotConfig.Load().Prefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;

            var context = new SocketCommandContext(client, message);

            var result = await commands.ExecuteAsync(context, argPos, _provider);

            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }



        private Task Log(LogMessage msg)
        {
            var cc = Console.ForegroundColor;
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now} [{msg.Severity,8}] {msg.Source}: {msg.ToString()}");
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }

        public static void EnsureBotConfigExists()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "configuration")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "configuration"));

            string loc = Path.Combine(AppContext.BaseDirectory, "configuration/config.json");

            if (!File.Exists(loc))                              // Check if the configuration file exists.
            {
                var config = new BotConfig();               // Create a new configuration object.

                Console.WriteLine("Please enter the following information to save into your configuration/config.json file");

                Console.Write("Bot Token: ");
                config.Token = Console.ReadLine();              // Read the bot token from console.

                Console.Write("Bot Prefix: ");
                config.Prefix = Console.ReadLine();              // Read the bot prefix from console.

                config.Save();                                  // Save the new configuration object to file.
            }
            Console.WriteLine("Configuration has been loaded");
        }
    }
}
