using CWSBot.Interaction;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CWSBot.Services
{
    public class MuteService
    {
        private const int PollRate = 1000;

        private readonly DiscordSocketClient _client;

        private readonly IConfiguration _config;

        private readonly Timer _checkTimer;
        
        public MuteService(DiscordSocketClient client, IConfiguration config)
        {
            this._checkTimer = new Timer((_) => Task.Run(() => CheckMutesAsync()), null, 0, Timeout.Infinite);
            _client = client;
            _config = config;
        }

        public bool TryAddMute(IUser actor, IUser target, IGuild guild, DateTimeOffset dueAt)
        {
            using (var dctx = new MuteContext())
            {
                if (dctx.Mutes.Any(x => x.MutedId == target.Id)) return false;

                dctx.Add(new Mute
                {
                    ActorId = actor.Id,
                    MutedId = target.Id,
                    DueAt = dueAt,
                    GuildId = guild.Id
                });

                dctx.SaveChanges();

                return true;
            }
        }

        public IEnumerable<Mute> GetMutes(IGuild guild)
        {
            using (var dctx = new MuteContext())
            {
                var mutes = dctx.Mutes.Where(x => x.GuildId == guild.Id);

                return mutes.OrderByDescending(x => x.DueAt).AsEnumerable();
            }
        }

        public bool RemoveQueuedUnmute(IUser user)
        {
            using (var dctx = new MuteContext())
            {
                var mute = dctx.Mutes.FirstOrDefault(x => x.MutedId == user.Id);

                if (mute is null) return false;

                dctx.Remove(mute);

                dctx.SaveChanges();

                return true;
            }
        }

        public async Task ForceUnmute(IUser user)
        {
            using (var dctx = new MuteContext())
            {
                var mute = dctx.Mutes.FirstOrDefault(x => x.MutedId == user.Id);

                if (mute is null) return;

                var guild = _client.GetGuild(mute.GuildId);

                var target = guild?.GetUser(mute.MutedId);

                var mutedRole = guild?.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_mute_name"].ToLower());

                if (target is null) return;

                if (mutedRole is null) return;

                try
                {
                    await target.RemoveRoleAsync(mutedRole);
                }
                catch { }

                RemoveQueuedUnmute(user);
            }
        }


        private async Task CheckMutesAsync()
        {
            try
            {
                using (var dctx = new MuteContext())
                {
                    var dueMutes = dctx.Mutes
                        .Where(x => x.DueAt.ToUnixTimeMilliseconds() <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                    if (dueMutes.Count() == 0)
                        return;

                    foreach (var mute in dueMutes)
                    {
                        var guild = _client.GetGuild(mute.GuildId);

                        var target = guild?.GetUser(mute.MutedId);

                        var mutedRole = guild?.Roles.FirstOrDefault(role => role.Name.ToLower() == _config["moderation_mute_name"].ToLower());

                        if (target is null) continue;

                        if (mutedRole is null) continue;

                        try
                        {
                            await target.RemoveRoleAsync(mutedRole);
                        } catch { }
                        continue;
                    }

                    dctx.Mutes.RemoveRange(dueMutes);

                    dctx.SaveChanges();
                }
            }
            finally
            {
                this._checkTimer.Change(PollRate, Timeout.Infinite);
            }
        }
    }
}
