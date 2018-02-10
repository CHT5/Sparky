using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
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
                LogLevel = DSharpPlus.LogLevel.Debug
            });

            var services = GetServices();

            var discordLogger = services.GetService<ILogger<DiscordClient>>();

            this._client.DebugLogger.LogMessageReceived += (sender, args) =>
            {
                switch (args.Level)
                {
                    case DSharpPlus.LogLevel.Info:
                        discordLogger.LogInformation(args.Message);
                        break;
                    case DSharpPlus.LogLevel.Critical:
                        discordLogger.LogCritical(args.Message);
                        break;
                    case DSharpPlus.LogLevel.Error:
                        discordLogger.LogError(args.Message);
                        break;
                    case DSharpPlus.LogLevel.Warning:
                        discordLogger.LogWarning(args.Message);
                        break;
                    case DSharpPlus.LogLevel.Debug:
                        discordLogger.LogDebug(args.Message);
                        break;
                }
            };

            _ = services.GetService<AuditLogService>(); // Let it instanciate

            commands = this._client.UseCommandsNext(new CommandsNextConfiguration
            {
                CaseSensitive = false,
                EnableMentionPrefix = true,
                StringPrefixes = new [] {this._config["prefix"]},
                Services = services
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
        {
            var provider = new ServiceCollection().AddDbContext<KarmaContext>(ServiceLifetime.Transient)
                                                  .AddDbContext<ModLogContext>(ServiceLifetime.Transient)
                                                  .AddSingleton<ILoggerFactory, LoggerFactory>()
                                                  .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                                                  .AddLogging(x => x.AddNLog(new NLogProviderOptions
                                                      {
                                                          CaptureMessageTemplates = true,
                                                          IgnoreEmptyEventId = true,
                                                          CaptureMessageProperties = true
                                                      }))
                                                  .AddSingleton(_client)
                                                  .AddSingleton(_config)
                                                  .AddSingleton<AuditLogService>()
                                                  .BuildServiceProvider();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            loggerFactory.ConfigureNLog(Path.Combine(Directory.GetCurrentDirectory(), "Files", "nlog.config")); // The # in my path is kicking my butt
            return provider;
        }
    }
}
