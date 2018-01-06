using CWSBot.Modules.Public;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CWSBot.Entities
{
    public class ModLog
    {
        [Key]
        public int Id { get; set; }

        public string Action { get; set; }

        public DateTimeOffset Time { get; set; } // trinit used DateTime at first smh

        public string Reason { get; set; }

        public ulong? MessageId { get; set; }

        public Severity Severity { get; set; }

        public ulong ActorId { get; set; }
    }
}
