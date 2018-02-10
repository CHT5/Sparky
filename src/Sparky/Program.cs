using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sparky.Data;
using Sparky.Services;

namespace Sparky
{
    class Program
    {
        static Task Main()
            => new Program().StartAsync();

        private DiscordClient _client;

        private IConfiguration _config;

        private CommandsNextExtension commands;

        private async Task StartAsync()
        {
            this._config = GetConfiguration();

            this._client = new DiscordClient(new DiscordConfiguration{
                TokenType = TokenType.Bot,
                Token = this._config["token"],
                LogLevel = LogLevel.Debug
            });

            this._client.DebugLogger.LogMessageReceived += (sender, msg) => Console.WriteLine(msg.ToString());

            var auditLogService = new AuditLogService(_client, _config);

            commands = this._client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefixes = new [] {this._config["prefix"]},
                Services = GetServices()
            });

            commands.RegisterCommands(Assembly.GetEntryAssembly());

            await this._client.ConnectAsync();

            await Task.Delay(-1);
        }

        private IConfiguration GetConfiguration()
            => new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                                         .AddJsonFile("Files/config.json")
                                         .Build();

        private IServiceProvider GetServices()
            => new ServiceCollection().AddDbContext<KarmaContext>(ServiceLifetime.Transient)
                                      .AddDbContext<ModLogContext>(ServiceLifetime.Transient)
                                      .AddSingleton(_client)
                                      .AddSingleton(_config)
                                      .BuildServiceProvider();
    }
}
