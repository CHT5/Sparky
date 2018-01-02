using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CWSBot.Entities.Interactive
{
    public interface IInteractiveMessage
    {
        IEnumerable<InteractiveMessageTrigger> Triggers { get; }

        Task SetUpAsync();

        Task OnTriggerReceived(SocketReaction reaction);

        void AddServiceProvider(IServiceProvider provider);
    }
}