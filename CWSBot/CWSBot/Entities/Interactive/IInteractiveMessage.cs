using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CWSBot.Entities.Interactive
{
    public interface IInteractiveMessage
    {
        /// <value>
        ///     A collection of <see cref="InteractiveMessageTrigger{T}"/>
        /// </value>
        IEnumerable<InteractiveMessageTrigger> Triggers { get; }

        /// <summary>
        ///     Sets up the message
        /// </summary>
        Task SetUpAsync();

        /// <summary>
        ///     This will be invoked when every <see cref="InteractiveMessageTrigger{T}"/> in <see cref="T:IInteractiveMessage.Triggers"/> is fulfilled.
        /// </summary>
        Task OnTriggerReceived(SocketReaction reaction);

        /// <summary>
        ///     Supplies an <see cref="T:IServiceProvider"/> to the class
        /// </summary>
        void AddServiceProvider(IServiceProvider provider);
    }
}