using Sparky.Data.Attributes;

namespace Sparky.Data
{
    public enum ModerationAction
    {
        Kick,
        Ban,
        Softban,
        Unban,
        SpecialRoleAdded,
        SpecialRoleRemoved,

        [TemporaryModAction]
        TemporarySpecialRoleAdded,
        [TemporaryModAction]
        TemporaryBan
    }
}