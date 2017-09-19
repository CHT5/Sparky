using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CWSBot.Interaction
{
    public class Tag
    {
        //primary key
        [Key]
        public int Id { get; set; }

        //name of the tag
        public string Name { get; set; }

        //id of the creator or current owner
        public ulong CreatorId { get; set; }

        //name of the creator or current owner
        public string CreatorName { get; set; }

        //time created
        public DateTimeOffset CreatedAt { get; set; }

        //content of the tag
        public string Content { get; set; }

        //id of the guild the tag is in
        public ulong GuildId { get; set; }

        //count the amount of times the tag was used
        public int Uses { get; set; }
    }
}
