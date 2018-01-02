using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace CWSBot.Entities.Interactive
{
    public interface ITriggerCondition
    {
        Task<bool> CheckAsync(SocketReaction reaction, IUserMessage message);
    }
}