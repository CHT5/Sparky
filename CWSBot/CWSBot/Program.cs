using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CWSBot.Config;
using System.IO;
using Discord.Addons.InteractiveCommands;
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
            client.UserLeft += UserLeft;
            client.UserJoined += UserJoined;
            string token = BotConfig.Load().Token;

            await InstallCommands();

            var serviceProvider = ConfigureServices();
            _provider = serviceProvider;
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task UserLeft(SocketGuildUser arg)
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
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<InteractiveService>()
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
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());
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