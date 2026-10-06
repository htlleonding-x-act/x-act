using XActBackend.Persistence.Model;

namespace XActBackend.Core.Util;

public static class MemberDisplayName
{
    public const string Unknown = "Unknown";

    /// <summary>the username of a registered member, the guest name otherwise</summary>
    public static string Resolve(TeamMember member, User? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.Username))
        {
            return user.Username;
        }

        return string.IsNullOrWhiteSpace(member.GuestName) ? Unknown : member.GuestName;
    }
}
