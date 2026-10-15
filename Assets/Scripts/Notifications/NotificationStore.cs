using System.Collections.Generic;
using System.Diagnostics;

namespace Notifications
{
    // Accessed only by the main-thread message dispatcher and UI.
    public static class NotificationStore
    {
        // Client presentation history only. Wire snapshot limits remain unchanged.
        public const int BossHistoryLimit = 20;
        private static readonly List<NotificationEntry> bosses = new List<NotificationEntry>();
        private static readonly List<NotificationEntry> bossHistory = new List<NotificationEntry>();
        private static readonly List<NotificationEntry> server = new List<NotificationEntry>();
        private static readonly List<NotificationEntry> invitations = new List<NotificationEntry>();
        private static readonly HashSet<long> unread = new HashSet<long>();
        private static long sequence, serverClock, clockAnchor;
        public static IReadOnlyList<NotificationEntry> Bosses => bosses;
        public static IReadOnlyList<NotificationEntry> BossHistory => bossHistory;
        public static IReadOnlyList<NotificationEntry> Server => server;
        public static IReadOnlyList<NotificationEntry> Invitations => invitations;
        public static int UnreadCount => unread.Count;
        public static int ServerUnreadCount => server.FindAll(n => unread.Contains(n.Id)).Count;
        public static int InvitationUnreadCount => invitations.FindAll(n => unread.Contains(n.Id)).Count;
        public static long PopupId { get; private set; }
        public static bool ReadingInvitations { get; set; }
        public static int Revision { get; private set; }
        public static bool HasSnapshot { get; private set; }
        public static bool ReadingServer { get; set; }
        public static long ServerNow => serverClock + (Stopwatch.GetTimestamp() - clockAnchor) * 1000 / Stopwatch.Frequency;

        public static void BeginSession()
        {
            bosses.Clear(); bossHistory.Clear(); server.Clear(); invitations.Clear(); unread.Clear();
            sequence = 0; HasSnapshot = false; ReadingServer = false; ReadingInvitations = false; PopupId = 0;
            if (GameScr.instance != null) GameScr.instance.popUpYesNo = null;
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
            var knownIds = new HashSet<long>();
            foreach (var n in server) knownIds.Add(n.Id);
            foreach (var n in invitations) knownIds.Add(n.Id);
            var previouslyUnread = new HashSet<long>(unread);
            bosses.Clear(); server.Clear(); invitations.Clear(); unread.Clear();
            foreach (NotificationEntry entry in entries)
            {
                if (entry.Kind == NotificationKind.Boss) { bosses.Insert(0, entry); RememberBoss(entry); }
                else if (entry.Kind == NotificationKind.Server) { server.Insert(0, entry); if (!ReadingServer) unread.Add(entry.Id); }
                else if (entry.ExpiresAt > time) { invitations.Insert(0, entry); if (!ReadingInvitations) unread.Add(entry.Id); }
            }
            unread.RemoveWhere(id => knownIds.Contains(id) && !previouslyUnread.Contains(id));
            if (invitations.Count > 0 && invitations[0].Id > PopupId) PopupId = invitations[0].Id;
            HasSnapshot = true;
            Advance(value, time);
        }

        public static bool Upsert(NotificationEntry entry, long value, long time)
        {
            if (!HasSnapshot || !IsNewer(value)) return true;
            List<NotificationEntry> target = Target(entry.Kind);
            int index = target.FindIndex(n => n.Id == entry.Id);
            // Never silently evict server news: ask for an authoritative snapshot instead.
            if (entry.Kind == NotificationKind.Server && index < 0 && target.Count >= 50) return false;
            if (index >= 0) target.RemoveAt(index);
            target.Insert(0, entry);
            if (entry.Kind == NotificationKind.Boss) {
                if (target.Count > 6) target.RemoveAt(target.Count - 1);
                RememberBoss(entry);
            }
            if (entry.Kind == NotificationKind.Server && !ReadingServer) unread.Add(entry.Id);
            if (entry.Kind == NotificationKind.Invitation) {
                if (!ReadingInvitations) unread.Add(entry.Id);
                PopupId = entry.Id;
            }
            Advance(value, time);
            return true;
        }

        public static void Remove(NotificationKind kind, long id, long value, long time)
        {
            if (!HasSnapshot || !IsNewer(value)) return;
            Target(kind).RemoveAll(n => n.Id == id);
            unread.Remove(id); Advance(value, time);
        }

        private static List<NotificationEntry> Target(NotificationKind kind) =>
            kind == NotificationKind.Boss ? bosses : kind == NotificationKind.Server ? server : invitations;

        private static void RememberBoss(NotificationEntry entry)
        {
            // Presentation history survives server retention REMOVE/resync within this session.
            // The server's grouping key is bossName + mapId: reuse that key even when a later
            // spawn receives a new ID after eviction. History never offers a travel action.
            int index = bossHistory.FindIndex(n => n.BossName == entry.BossName && n.MapId == entry.MapId);
            if (index >= 0) {
                if (bossHistory[index].UpdatedAt > entry.UpdatedAt) return;
                bossHistory.RemoveAt(index);
            }
            int position = bossHistory.FindIndex(n => n.UpdatedAt < entry.UpdatedAt);
            bossHistory.Insert(position < 0 ? bossHistory.Count : position, entry);
            if (bossHistory.Count > BossHistoryLimit) bossHistory.RemoveAt(bossHistory.Count - 1);
        }

        public static void MarkServerRead() { foreach (var n in server) unread.Remove(n.Id); }
        public static void MarkInvitationsRead() { foreach (var n in invitations) unread.Remove(n.Id); }
        public static NotificationEntry FindInvitation(long id) => invitations.Find(n => n.Id == id);

        public static void RespondToInvitation(long id, bool accept)
        {
            NotificationEntry entry = FindInvitation(id);
            if (entry == null || entry.Responding || entry.ExpiresAt <= ServerNow) return;
            if (!Service.gI().respondToInvitation(id, accept)) return;
            entry.Responding = true; entry.RespondedAt = ServerNow; Revision++;
            unread.Remove(id);
        }

        // Monotonic server-relative clock continues while panels or combat are active.
        public static void UpdateInvitations()
        {
            if (!HasSnapshot) return;
            long now = ServerNow;
            foreach (var n in invitations.ToArray()) {
                if (n.ExpiresAt <= now) { invitations.Remove(n); unread.Remove(n.Id); Revision++; }
                else if (n.Responding && now - n.RespondedAt >= 3000) {
                    n.RespondedAt = now; Service.gI().requestNotificationSnapshot();
                }
            }
            if (GameCanvas.currentScreen != GameScr.instance || GameScr.instance == null) return;
            NotificationEntry newest = FindInvitation(PopupId);
            // The latest item may have expired/been answered while its popup was hidden.
            // Fall back to the most recent still-valid invitation without changing responses.
            if (newest == null || newest.Responding)
                newest = invitations.Find(n => !n.Responding && n.ExpiresAt > now);
            if (newest == null || newest.Responding) GameScr.instance.popUpYesNo = null;
            else if (GameScr.instance.popUpYesNo == null || GameScr.instance.popUpYesNo.InvitationId != newest.Id)
                GameScr.instance.popUpYesNo = new PopUpYesNo(newest);
            GameScr.instance.popUpYesNo?.update();
        }
    }
}
