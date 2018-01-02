using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CWSBot.Entities.Interactive
{
    public class UserTriggerCondition : ITriggerCondition
    {
        private readonly ulong _userId;

        public UserTriggerCondition(IUser user) : this(user.Id)
        {}

        public UserTriggerCondition(ulong userId)
            => this._userId = userId;

        Task<bool> ITriggerCondition.CheckAsync(SocketReaction reaction, IUserMessage message)
            => Task.FromResult(this._userId == reaction.UserId);
    }
}