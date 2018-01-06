using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        private readonly List<IInteractiveMessage> _interactiveMessages;

        private readonly SemaphoreSlim _interactiveMessagesSemaphore = new SemaphoreSlim(1, 1);

        public InteractiveService(IServiceProvider serviceProvider)
        {
            this._client = serviceProvider.GetService<DiscordSocketClient>();
            this._client.ReactionAdded += (message, channel, reaction) => {_ = Task.Run(() => HandleReactionsAsync(message, channel, reaction)); return Task.CompletedTask;};
            this._provider = serviceProvider;
            this._interactiveMessages = new List<IInteractiveMessage>();
        }

        public async Task SendInteractiveMessageAsync(IInteractiveMessage message)
        {
            try
            {
                await this._interactiveMessagesSemaphore.WaitAsync();
                message.Exited += RemoveMessageAsync;
                message.AddServiceProvider(_provider);
                this._interactiveMessages.Add(message);
                await message.SetUpAsync();
            }
            finally
            {
                this._interactiveMessagesSemaphore.Release();
            }
        }

        private async Task HandleReactionsAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.UserId == this._client.CurrentUser.Id) return;

            foreach (var interactiveMessage in this._interactiveMessages.ToList())
            {
                if (!interactiveMessage.Triggers.Any(x => x.Trigger.ToLowerInvariant() == reaction.Emote.Name.ToLowerInvariant()))
                    continue;

                var msg = await message.GetOrDownloadAsync();

                var user = reaction.User.GetValueOrDefault();

                var checks = interactiveMessage.Triggers.SelectMany(x => x.Conditions.Select(y => y.CheckAsync(reaction, msg)));

                var results = await Task.WhenAll(checks);

                if (!results.All(x => x)) continue;

                await msg.RemoveReactionAsync(reaction.Emote, user);

                await interactiveMessage.OnTriggerReceived(reaction);
            }
        }

        private async Task RemoveMessageAsync(IInteractiveMessage message)
        {
            try
            {
                await this._interactiveMessagesSemaphore.WaitAsync();
                this._interactiveMessages.Remove(message);
            }
            finally
            {
                this._interactiveMessagesSemaphore.Release();
            }
        }
    }
}