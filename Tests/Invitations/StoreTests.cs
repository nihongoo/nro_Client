using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Notifications;

// Minimal dependencies; compile the actual models, store, protocol and byte reader.
namespace UnityEngine {
    public class TextAsset { public byte[] bytes; }
    public static class Resources { public static object Load(string name, Type type) { throw new NotSupportedException(); } }
}
public static class mSystem {
    public static sbyte[] convertToSbyte(byte[] bytes) { var result = new sbyte[bytes.Length]; Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length); return result; }
}
public class Message {
    private readonly myReader input;
    public Message(byte[] bytes) { input = new myReader(mSystem.convertToSbyte(bytes)); }
    public myReader reader() { return input; }
}
public class GameScr { public static GameScr instance = new GameScr(); public PopUpYesNo popUpYesNo; }
public static class GameCanvas { public static object currentScreen = GameScr.instance; }
public class PopUpYesNo {
    public long InvitationId;
    public PopUpYesNo(NotificationEntry entry) { InvitationId = entry.Id; }
    public void update() { }
}
public class Service {
    private static readonly Service instance = new Service();
    public static Service gI() { return instance; }
    public int responses, snapshots;
    public bool sendSucceeds = true;
    public bool respondToInvitation(long id, bool accept) { if (!sendSucceeds) return false; responses++; return true; }
    public void requestNotificationSnapshot() { snapshots++; }
}
public static class StoreTests {
    private static int assertions;
    private static void Check(bool valid, string message) { assertions++; if (!valid) throw new Exception(message); }
    private static NotificationEntry Invite(long id, long expires = 31000) {
        return new NotificationEntry { Id = id, Kind = NotificationKind.Invitation, UpdatedAt = 1000, ExpiresAt = expires, Text = "Lời mời " + id };
    }
    public static void Main(string[] args) {
        NotificationStore.BeginSession();
        NotificationStore.Snapshot(new List<NotificationEntry>(), 1, 1000);
        NotificationStore.Upsert(new NotificationEntry { Id = 1, Kind = NotificationKind.Boss }, 2, 1000);
        Check(NotificationStore.UnreadCount == 0, "Boss must have no badge");
        NotificationStore.Upsert(new NotificationEntry { Id = 2, Kind = NotificationKind.Server }, 3, 1000);
        NotificationStore.Upsert(Invite(3), 4, 1000);
        NotificationStore.Upsert(Invite(4), 5, 1000);
        NotificationStore.UpdateInvitations();
        Check(NotificationStore.Invitations.Count == 2 && GameScr.instance.popUpYesNo.InvitationId == 4, "newest popup, older in tab");
        Check(NotificationStore.UnreadCount == 3 && NotificationStore.ServerUnreadCount == 1 && NotificationStore.InvitationUnreadCount == 2, "badge categories");
        NotificationStore.MarkServerRead();
        Check(NotificationStore.UnreadCount == 2, "Server read cleared invitation badges");
        NotificationStore.RespondToInvitation(3, false); NotificationStore.RespondToInvitation(3, true);
        Check(Service.gI().responses == 1, "double response from row/popup");
        NotificationStore.RespondToInvitation(4, true); NotificationStore.UpdateInvitations();
        Check(GameScr.instance.popUpYesNo == null, "older invitation resurfaced as popup");
        NotificationStore.Remove(NotificationKind.Invitation, 4, 6, 1000);
        Check(NotificationStore.Invitations.Count == 1, "remove newest removed older");
        NotificationStore.Snapshot(new List<NotificationEntry> { Invite(3), Invite(5) }, 7, 1000);
        NotificationStore.UpdateInvitations();
        Check(GameScr.instance.popUpYesNo.InvitationId == 5 && NotificationStore.InvitationUnreadCount == 1, "resync lost newest popup/badge or marked old read entry unread");
        NotificationStore.Remove(NotificationKind.Invitation, 5, 6, 1000);
        Check(NotificationStore.FindInvitation(5) != null, "stale packet changed store");
        Service.gI().sendSucceeds = false; NotificationStore.RespondToInvitation(5, true);
        Check(!NotificationStore.FindInvitation(5).Responding, "failed send locked response"); Service.gI().sendSucceeds = true;
        NotificationStore.FindInvitation(5).ExpiresAt = NotificationStore.ServerNow;
        NotificationStore.UpdateInvitations();
        Check(NotificationStore.FindInvitation(5) == null && GameScr.instance.popUpYesNo == null, "expiry while panel/combat active");
        NotificationStore.FindInvitation(3).Responding = true;
        NotificationStore.FindInvitation(3).RespondedAt = NotificationStore.ServerNow - 3100;
        NotificationStore.UpdateInvitations(); Check(Service.gI().snapshots == 1, "missing response recovery");
        NotificationStore.BeginSession(); Check(NotificationStore.Invitations.Count == 0 && GameScr.instance.popUpYesNo == null, "session isolation");
        // Consume a binary UPSERT produced by the Java registry test, including Vietnamese UTF-8.
        byte[] packet = File.ReadAllBytes(args[0]); var header = new Message(packet).reader();
        Check(header.readByte() == 2 && header.readByte() == 0, "wire version/action");
        long sequence = header.readLong(), time = header.readLong();
        NotificationStore.Snapshot(new List<NotificationEntry>(), sequence - 1, time);
        NotificationProtocol.Read(new Message(packet));
        Check(NotificationStore.Invitations.Count == 1, "Java packet not decoded");
        var wire = NotificationStore.Invitations[0];
        Check(wire.InvitationType == 2 && wire.SenderId == 1 && wire.Text == "Lời mời 2", "Java/C# payload mismatch");
        Check(wire.ExpiresAt - wire.UpdatedAt == 30000, "wire TTL");
        int revision = NotificationStore.Revision;
        NotificationProtocol.Read(new Message(new byte[] { 2, 0 }));
        Check(NotificationStore.Revision == revision && Service.gI().snapshots == 2, "malformed packet corrupted store");
        Console.WriteLine("PASS: " + assertions + " client assertions; Java wire fixture, badges, popup order, replay, resync, expiry and session reset");
    }
}
