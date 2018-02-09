using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;

namespace Sparky
{
    class Program
    {
        static Task Main()
            => new Program().StartAsync();

        private IConfiguration _config;

        private CommandsNextExtension commands;

        private async Task StartAsync()
        {
            this._config = GetConfiguration();

            var client = new DiscordClient(new DiscordConfiguration{
                TokenType = TokenType.Bot,
                Token = this._config["token"],
                LogLevel = LogLevel.Debug
            });

            client.DebugLogger.LogMessageReceived += (sender, msg) => Console.WriteLine(msg.ToString());

            commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefixes = new [] {this._config["prefix"]}
            });

            commands.RegisterCommands(Assembly.GetEntryAssembly());

            await client.ConnectAsync();

            await Task.Delay(-1);
        }

        private IConfiguration GetConfiguration()
            => new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                         .AddJsonFile("Files/config.json")
                                         .Build();
    }
}
