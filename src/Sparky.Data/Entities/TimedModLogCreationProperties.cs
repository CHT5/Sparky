using System;

namespace Sparky.Data
{
    public class TimedModLogCreationProperties : ModLogCreationProperties
    {
        public DateTimeOffset EndsAt { get; set; }
    }
}