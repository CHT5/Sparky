using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using DSharpPlus.Entities;
using System.Linq;

namespace Sparky.Attributes
{
    public class RequireConfiguredRoleAttribute : CheckBaseAttribute
    {
        private readonly IEnumerable<string> _paths;

        private readonly RoleCheckMode _checkMode;

        public bool IsDMExecutable { get; set; } = false;

        public bool IgnoreNotFoundRoles { get; set; } = false;

        public bool IsCaseSensitive { get; set; } = false;

        public RequireConfiguredRoleAttribute(RoleCheckMode mode, params string[] paths)
        {
            this._checkMode = mode;
            this._paths = paths;
        }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var config = ctx.Services.GetRequiredService<IConfiguration>();

            if (ctx.Channel.IsPrivate && !this.IsDMExecutable)
                return Task.FromResult(false);
            else if (ctx.Channel.IsPrivate && this.IsDMExecutable)
                return Task.FromResult(true);

            var results = new List<bool>();

            foreach (var path in this._paths)
            {
                var res = config[path];

                DiscordRole role = null;

                if (ulong.TryParse(res, out ulong roleId))
                    role = ctx.Guild.GetRole(roleId);
                else
                    role = ctx.Guild.Roles.FirstOrDefault(x => (this.IsCaseSensitive ? x.Name : x.Name.ToLower()) 
                                                                == (this.IsCaseSensitive ? res : res.ToLower()));

                if (role is null)
                {
                    if (this.IgnoreNotFoundRoles)
                        results.Add(true);
                    else
                        results.Add(false);

                    continue;
                }

                var hasRole = (ctx.User as DiscordMember).Roles.Any(x => x.Id == role.Id);
                results.Add(hasRole);
            }

            switch (this._checkMode)
            {
                case RoleCheckMode.All:
                    return Task.FromResult(results.All(x => x));
                case RoleCheckMode.Any:
                    return Task.FromResult(results.Any(x => x));
                case RoleCheckMode.None:
                    return Task.FromResult(!results.Any(x => x));
                default:
                    throw new Exception($"Invalid {nameof(RoleCheckMode)}");
            }
        }
    }
}