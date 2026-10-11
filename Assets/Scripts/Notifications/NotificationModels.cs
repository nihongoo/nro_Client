namespace Notifications
{
    public enum NotificationKind : byte { Boss = 0, Server = 1 }

    // Static news only: no live tracking, navigation or gameplay actions.
    public sealed class NotificationEntry
    {
        public long Id, UpdatedAt;
        public NotificationKind Kind;
        public string BossName, MapName, Killer, Category, Text;
        public int MapId;
        public bool Killed;
    }
}
