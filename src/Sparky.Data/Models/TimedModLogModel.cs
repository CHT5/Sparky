using System;
using Microsoft.EntityFrameworkCore;

namespace Sparky.Data.Models
{
    internal class TimedModLogModel : ModLogModel
    {
        public DateTimeOffset EndsAt { get; set; }

        public bool Completed { get; set; }
    }
}