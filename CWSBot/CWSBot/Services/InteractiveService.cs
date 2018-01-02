using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using CWSBot.Entities.Interactive;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace CWSBot.Services
{
    public class InteractiveService
    {
        private readonly DiscordSocketClient _client;

        private readonly IServiceProvider _provider;

        private readonly ConcurrentBag<IInteractiveMessage> _interactiveMessages;

        public InteractiveService(IServiceProvider serviceProvider)
        {
            this._client = serviceProvider.GetService<DiscordSocketClient>();
            this._client.ReactionAdded += HandleReactionsAsync;
            this._provider = serviceProvider;
            this._interactiveMessages = new ConcurrentBag<IInteractiveMessage>();
        }

        public Task SendInteractiveMessageAsync(IInteractiveMessage message)
        {
            message.AddServiceProvider(_provider);
            this._interactiveMessages.Add(message);
            return message.SetUpAsync();
        }

        private async Task HandleReactionsAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == this._client.CurrentUser.Id) return;

            foreach (var interactiveMessage in this._interactiveMessages)
            {
                if (!interactiveMessage.Triggers.Any(x => x.Trigger.ToLowerInvariant() == reaction.Emote.Name.ToLowerInvariant()))
                    continue;

                var msg = await message.GetOrDownloadAsync();

                var user = reaction.User.GetValueOrDefault();

                await msg.RemoveReactionAsync(reaction.Emote, user);

                var checks = interactiveMessage.Triggers.SelectMany(x => x.Conditions.Select(y => y.CheckAsync(reaction, msg)));

                var results = await Task.WhenAll(checks);

                if (!results.All(x => x)) continue;

                await interactiveMessage.OnTriggerReceived(reaction);
            }
        }
    }
}