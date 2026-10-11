using System.Collections.Generic;
using System.Diagnostics;

namespace Notifications
{
    // Accessed only by the main-thread message dispatcher and UI.
    public static class NotificationStore
    {
        private static readonly List<NotificationEntry> bosses = new List<NotificationEntry>();
        private static readonly List<NotificationEntry> server = new List<NotificationEntry>();
        private static readonly HashSet<long> unread = new HashSet<long>();
        private static long sequence, serverClock, clockAnchor;
        public static IReadOnlyList<NotificationEntry> Bosses => bosses;
        public static IReadOnlyList<NotificationEntry> Server => server;
        public static int UnreadCount => unread.Count;
        public static int Revision { get; private set; }
        public static bool HasSnapshot { get; private set; }
        public static bool ReadingServer { get; set; }
        public static long ServerNow => serverClock + (Stopwatch.GetTimestamp() - clockAnchor) * 1000 / Stopwatch.Frequency;

        public static void BeginSession()
        {
            bosses.Clear(); server.Clear(); unread.Clear();
            sequence = 0; HasSnapshot = false; ReadingServer = false;
            Revision++;
        }

        public static bool IsNewer(long value) { return value > sequence; }
        private static void Advance(long value, long time)
        {
            sequence = value; serverClock = time; clockAnchor = Stopwatch.GetTimestamp(); Revision++;
        }

        public static void Snapshot(List<NotificationEntry> entries, long value, long time)
        {
            if (HasSnapshot && !IsNewer(value)) return;
            bosses.Clear(); server.Clear(); unread.Clear();
            foreach (NotificationEntry entry in entries)
            {
                if (entry.Kind == NotificationKind.Boss) bosses.Insert(0, entry);
                else { server.Insert(0, entry); if (!ReadingServer) unread.Add(entry.Id); }
            }
            HasSnapshot = true;
            Advance(value, time);
        }

        public static bool Upsert(NotificationEntry entry, long value, long time)
        {
            if (!HasSnapshot || !IsNewer(value)) return true;
            List<NotificationEntry> target = entry.Kind == NotificationKind.Boss ? bosses : server;
            int index = target.FindIndex(n => n.Id == entry.Id);
            // Never silently evict server news: ask for an authoritative snapshot instead.
            if (entry.Kind == NotificationKind.Server && index < 0 && target.Count >= 50) return false;
            if (index >= 0) target.RemoveAt(index);
            target.Insert(0, entry);
            if (entry.Kind == NotificationKind.Boss && target.Count > 6) target.RemoveAt(target.Count - 1);
            if (entry.Kind == NotificationKind.Server && !ReadingServer) unread.Add(entry.Id);
            Advance(value, time);
            return true;
        }

        public static void Remove(NotificationKind kind, long id, long value, long time)
        {
            if (!HasSnapshot || !IsNewer(value)) return;
            (kind == NotificationKind.Boss ? bosses : server).RemoveAll(n => n.Id == id);
            unread.Remove(id); Advance(value, time);
        }

        public static void MarkServerRead() { unread.Clear(); }
    }
}
