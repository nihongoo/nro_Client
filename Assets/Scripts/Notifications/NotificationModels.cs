namespace Notifications
{
    public enum NotificationKind : byte { Boss = 0, Server = 1, Invitation = 2 }

    public sealed class NotificationEntry
    {
        public long Id, UpdatedAt, ExpiresAt, RespondedAt;
        public NotificationKind Kind;
        public string BossName, MapName, Killer, Category, Text;
        public int MapId;
        public bool Killed, Responding;
        public byte InvitationType;
        public int SenderId;
    }
}
