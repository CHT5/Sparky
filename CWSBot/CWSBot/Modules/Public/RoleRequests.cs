using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Diagnostics;
using CWSBot.Interaction;
using Microsoft.Extensions.Configuration;

namespace CWSBot.Modules.Public
{
    public class RoleRequests : ModuleBase<SocketCommandContext>
    {
        private const ulong nsfw_role_id = 340494268298690561;
        private const string AcceptedEmoji = "\u2611";

        [Command("nsfw")]
        public async Task ToggleNsfw()
        {
            var user = Context.User as SocketGuildUser;

            var nsfwRole = Context.Guild.Roles.FirstOrDefault(role => role.Id == nsfw_role_id);

            if (user.Roles.Any(role => role.Id == nsfwRole.Id))
                await user.RemoveRoleAsync(nsfwRole);
            else
                await user.AddRoleAsync(nsfwRole);

            await Context.Message.AddReactionAsync(new Emoji(AcceptedEmoji));
        }
    }
}
