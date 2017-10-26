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
using CWSBot.Interaction;
using CWSBot.Services;

namespace CWSBot
{
    class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _provider;

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            EnsureBotConfigExists(); // Ensure that the bot configuration json file has been created.

            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
            });

            _commands = new CommandService(new CommandServiceConfig
                {
                    CaseSensitiveCommands = false
                });

            _client.Log += Log;
            _commands.Log += Log;
            _client.MessageReceived += HandleMessageReceived;

            await InstallCommands();

            _provider = ConfigureServices();

            var token = BotConfig.Load().Token;
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task HandleMessageReceived(SocketMessage arg)
        {
            _ = Task.Run(() => MessageReceivedHandler(arg));
            return Task.CompletedTask;
        }

        private void MessageReceivedHandler(SocketMessage arg)
            => _ = Task.Run(() => AddUserToDb(arg)).ConfigureAwait(false);

        private Task AddUserToDb(SocketMessage arg)
        {
            using (var dctx = new CwsContext())
            {
                User DbUser = dctx.Users.SingleOrDefault(x => x.UserId == arg.Author.Id);

                if (DbUser is null)
                {
                    User NewDbUser = new User
                    {
                        UserId = arg.Author.Id,
                        Karma = 0,
                        WarningCount = 0,
                        MessageCount = 1,
                        Tokens = 0,
                        KarmaTime = DateTimeOffset.Now.AddDays(-1),
                        Username = arg.Author.Username
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
            => new ServiceCollection()
                .AddSingleton(_client)
                .AddDbContext<CwsContext>()
                .AddSingleton(_commands)
                .BuildServiceProvider();

        public async Task InstallCommands()
        {
            _client.MessageReceived += HandleCommand;
            _client.Ready += OnConnected;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task OnConnected()
        {
            //Set game status.
            await _client.SetGameAsync(BotConfig.Load().Prefix + "help");
        }
        
        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;

            int argPos = 1;
            if (!(message.HasStringPrefix(BotConfig.Load().Prefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            var context = new SocketCommandContext(_client, message);
            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (!result.IsSuccess)
                Console.WriteLine(result.ErrorReason);
        }

        private Task Log(LogMessage msg)
        {
            var colorOld = Console.ForegroundColor;
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
            Console.ForegroundColor = colorOld;

            return Task.CompletedTask;
        }

        // Can this whole config shit be replaced by Microsoft.Extensions.Configuration.Json? Thy
        public static void EnsureBotConfigExists()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "configuration")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "configuration"));

            string filePath = Path.Combine(AppContext.BaseDirectory, "configuration/config.json");

            if (!File.Exists(filePath))                              // Check if the configuration file exists.
            {
                var config = new BotConfig();               // Create a new configuration object.

                Console.WriteLine("Please enter the following information to save into your configuration/config.json file");
                Console.Write("Bot Token: ");

                var inputToken = Console.ReadLine();
                while(inputToken == string.Empty)
                {
                    Console.Write("Please enter a valid token: ");
                    inputToken = Console.ReadLine();
                }

                config.Token = inputToken;

                Console.Write("Bot Prefix: ");
                var inputPrefix = Console.ReadLine();
                if (inputPrefix == string.Empty)
                    Console.WriteLine("Using default prefix: {0}", config.Prefix);
                else
                    config.Prefix = inputPrefix;

                config.Save();
            }

            Console.WriteLine("Configuration has been loaded");
        }
    }
}
