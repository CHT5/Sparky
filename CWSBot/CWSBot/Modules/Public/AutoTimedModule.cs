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

        [Command("info")]
        public async Task PacktPub()
        {
            var application = await Context.Client.GetApplicationInfoAsync();  /*for lib version*/
            using (var process = Process.GetCurrentProcess())
            {
                var time = DateTime.Now - process.StartTime;
                string upTime = "";
                // application.Owner.Name
            }
        }
        // start here with webscrapig from packtpub


    }
}