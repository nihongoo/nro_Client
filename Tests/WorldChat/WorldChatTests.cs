using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Notifications;
using WorldChat;

public static class WorldChatTests
{
    private static int assertions;
    private static void Check(bool value, string text) { assertions++; if (!value) throw new Exception(text); }
    private static WorldChatEntry Entry(long id) => new WorldChatEntry { Id = id, SentAt = 1000, SenderId = 1, Sender = "Tên", Text = "Tin " + id };
    public static void Main(string[] args)
    {
        NotificationStore.BeginSession();
        NotificationStore.Snapshot(new List<NotificationEntry> { new NotificationEntry { Id = 99, Kind = NotificationKind.Server, Text = "News" } }, 1, 1000);
        WorldChatStore.BeginSession();
        Check(!WorldChatStore.Ready && WorldChatStore.GemFee == 0, "client must wait for authoritative policy");
        Check(WorldChatStore.StartRequest() == 0 && WorldChatStore.Validate("test") != null, "unconfigured submission allowed");
        byte[] packet = File.ReadAllBytes(args[0]);
        NotificationProtocol.Read(new Message(packet));
        Check(WorldChatStore.Ready && WorldChatStore.Entries.Count == 30, "Java snapshot not routed/read");
        Check(WorldChatStore.GemFee == 10 && WorldChatStore.GoldFee == 10000000L, "fee wire mismatch");
        Check(WorldChatStore.MaxCharacters == 100 && WorldChatStore.CooldownMs == 5000, "limits wire mismatch");
        Check(WorldChatStore.Entries[0].Id == 6 && WorldChatStore.Entries[29].Id == 35, "snapshot order");
        Check(WorldChatStore.Entries[29].Text == "Tin số 35 😀", "Java modified UTF text/surrogate lost");
        byte[] utfPacket = File.ReadAllBytes(args[1]);
        Check(WorldChatProtocol.ReadText(new myReader(mSystem.convertToSbyte(utfPacket))) == "Xin chào 😀", "Java modified UTF decode");
        var writer = new myWriter(); WorldChatProtocol.WriteText(writer, "Xin chào 😀");
        sbyte[] encoded = writer.getData(); byte[] actual = new byte[encoded.Length]; Buffer.BlockCopy(encoded, 0, actual, 0, encoded.Length);
        Check(Convert.ToBase64String(actual) == Convert.ToBase64String(utfPacket), "client encoder differs from Java writeUTF");
        Check(WorldChatStore.FeeLabel.Contains("10 gem") && WorldChatStore.FeeLabel.Contains("10.000.000"), "fee label");
        Check(NotificationStore.UnreadCount == 1 && NotificationStore.Server.Count == 1, "world chat changed notification badge");
        int revision = WorldChatStore.Revision;
        var malformed = (byte[])packet.Clone(); malformed[45] = 0; malformed[46] = 99;
        NotificationProtocol.Read(new Message(malformed));
        Check(WorldChatStore.Revision == revision && WorldChatStore.Entries.Count == 30, "malformed snapshot partially replaced store");
        Check(Service.gI().worldSnapshots == 1 && Service.gI().snapshots == 0, "world malformed packet requested wrong snapshot");
        Check(WorldChatStore.Append(Entry(36), 36, WorldChatStore.Now), "append failed");
        Check(WorldChatStore.Entries.Count == 30 && WorldChatStore.Entries[29].Id == 36 && WorldChatStore.Entries[0].Id == 7, "retention/latest");
        Check(WorldChatStore.Append(Entry(36), 36, 2000) && WorldChatStore.Entries.Count == 30, "duplicate append");
        Check(!WorldChatStore.Append(Entry(38), 38, 2000) && WorldChatStore.Entries[29].Id == 36, "gap silently accepted");
        NotificationProtocol.Read(new Message(packet));
        Check(WorldChatStore.Entries[29].Id == 36, "stale snapshot rolled back history");
        Check(WorldChatStore.Validate("  ") != null && WorldChatStore.Validate("\u00a0") != null, "blank accepted");
        Check(WorldChatStore.Validate(new string('x', 101)) != null, "over limit accepted");
        Check(WorldChatStore.Validate("line\nline") != null, "control characters accepted");
        Check(WorldChatStore.Validate(" Xin chào ") == null, "valid text rejected");
        Check(WorldChatStore.CharacterCount("😀😀") == 2 && WorldChatStore.CharacterCount("Tiếng Việt") == 10, "Unicode counting");
        long id = WorldChatStore.StartRequest();
        Check(id > 0 && WorldChatStore.StartRequest() == 0 && WorldChatStore.Validate("test") != null, "duplicate send enabled");
        Check(WorldChatStore.Result(id + 1, true, "stale result", 7000, 36, 2000) && WorldChatStore.PendingId == id, "unmatched result cleared pending");
        Check(WorldChatStore.Result(id, true, "ok", 7000, 36, 2000) && WorldChatStore.PendingId == 0, "result not matched");
        Check(WorldChatStore.ResultAccepted && WorldChatStore.StartRequest() == 0, "server cooldown ignored");
        WorldChatStore.Snapshot(new List<WorldChatEntry> { Entry(36) }, 36, 8000, 17, 123456, 80, 7000, 12, 8000);
        Check(WorldChatStore.GemFee == 17 && WorldChatStore.GoldFee == 123456 && WorldChatStore.MaxCharacters == 80 && WorldChatStore.HistoryLimit == 12, "client hardcoded policy");
        id = WorldChatStore.StartRequest(); WorldChatStore.SendFailed(id);
        Check(WorldChatStore.PendingId == 0, "local send failure locked input");
        id = WorldChatStore.StartRequest();
        typeof(WorldChatStore).GetField("pendingSince", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0L);
        Check(WorldChatStore.CheckTimeout() && WorldChatStore.PendingId == 0 && !WorldChatStore.ResultAccepted, "timeout not recovered");
        Check(WorldChatStore.ResultText.Contains("kiểm tra tin"), "timeout encouraged blind duplicate charge");
        WorldChatStore.BeginSession();
        Check(!WorldChatStore.Ready && WorldChatStore.Entries.Count == 0 && WorldChatStore.PendingId == 0, "login leaked history/pending/policy");
        Console.WriteLine("PASS: " + assertions + " world-chat client assertions; Java packet, policy, UTF, retention, badge isolation, replay/gaps, pending/cooldown and session reset");
    }
}
