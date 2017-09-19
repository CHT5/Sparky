using Newtonsoft.Json;
using System;
using System.IO;

namespace CWSBot.Config
{
    public class BotConfig
    {
        [JsonIgnore]
        public static readonly string appdir = AppContext.BaseDirectory;

        public string Prefix { get; set; }
        public string Token { get; set; }

        private static BotConfig _config = null;

        public BotConfig()
        {
            Prefix = "!";
            Token = "";
        }

        public void Save(string dir = "configuration/config.json")
        {
            string filePath = Path.Combine(appdir, dir);
            File.WriteAllText(filePath, ToJson());
        }

        public static BotConfig Load(string dir = "configuration/config.json")
        {
            return _config ?? (_config = JsonConvert.DeserializeObject<BotConfig>(File.ReadAllText(Path.Combine(appdir, dir))));
        }

        public string ToJson()
            => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}