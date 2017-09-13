using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CWSBot.Config;
using System.Diagnostics;

namespace CWSBot.Modules.Public
{
    public class AutoTimedModule : ModuleBase<SocketCommandContext>
    {

        private CommandService _service;
        public AutoTimedModule(CommandService service)
        {
            _service = service;
        }


        // start here with webscrapig from packtpub



    }
}