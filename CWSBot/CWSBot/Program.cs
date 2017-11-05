using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using CWSBot;
using System.Linq;
using CWSBot.Interaction;
using CWSBot.Services;
using Microsoft.Extensions.Configuration;

namespace CWSBot
{
    class Program
    {
        private CommandService _commands;
        private DiscordSocketClient _client;
        private IServiceProvider _provider;
        private IConfiguration _config;

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            if (!File.Exists("Files/config.json"))
            {
                Console.WriteLine("Please populate the config.json");
                Environment.Exit(1);
            }

            _config = GetConfiguration();

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

            await _client.LoginAsync(TokenType.Bot, _config["token"]);
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

        

        private IServiceProvider ConfigureServices()
            => new ServiceCollection()
                .AddSingleton(_client)
                .AddDbContext<CwsContext>()
                .AddSingleton(_commands)
                .AddSingleton(new NameService(_client))
                .AddSingleton(_config)
                .AddSingleton(new AnnounceService(_client, _config))
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
            await _client.SetGameAsync(_config["prefix"] + "help");
        }
        
        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null || message.Author.IsBot) return;

            int argPos = 1;
            if (!(message.HasStringPrefix(_config["prefix"], ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

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

        private IConfiguration GetConfiguration()
            => new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("Files/config.json", false, true)
                   .Build();
    }
}
