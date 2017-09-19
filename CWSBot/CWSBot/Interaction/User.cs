using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CWSBot.Interaction
{
    public class User
    {
        //primary key
        [Key]
        public int Id { get; set; }

        //ulongs
        public ulong UserId { get; set; }

        //integers
        public int Karma { get; set; }
        public int WarningCount { get; set; }
        public int MessageCount { get; set; }

        //uints
        public uint Tokens { get; set; }
        //DateTimeOffsets (to add time delays to giving karma)

        public DateTimeOffset KarmaTime { get; set; }

        //not sure why we store this but sure?
        public string Username { get; set; }
    }
}
