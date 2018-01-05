using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CWSBot.Entities.Interactive
{
    public class MessageTriggerCondition : ITriggerCondition
    {
        private readonly ulong _messageId;

        public MessageTriggerCondition(IMessage message) : this(message.Id)
        {}

        public MessageTriggerCondition(ulong messageId)
            => this._messageId = messageId;

        Task<bool> ITriggerCondition.CheckAsync(SocketReaction reaction, IUserMessage message)
            => Task.FromResult(message.Id == this._messageId);
    }
}