using Discord.Commands;

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
