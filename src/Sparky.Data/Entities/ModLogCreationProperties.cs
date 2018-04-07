namespace Sparky.Data
{
    public class ModLogCreationProperties
    {
        public ulong ResponsibleUserId { get; set; }

        public ulong GuildId { get; set; }

        public ulong? AuditLogId { get; set; }

        public ModerationAction Action { get; set; }

        public ulong UserId { get; set; }

        public string TargetUsername { get; set; }

        public string TargetDiscriminator { get; set; }

        public ulong? RoleAdded { get; set; }

        public string Reason { get; set; }
    }
}