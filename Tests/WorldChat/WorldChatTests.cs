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
        Check(!WorldChatStore.Ready && WorldChatStore.GemFee == 0, "wait for server policy");
        Check(WorldChatStore.StartRequest() == 0 && WorldChatStore.Validate("test") != null, "unconfigured submission allowed");
        byte[] packet = File.ReadAllBytes(args[0]);
        NotificationProtocol.Read(new Message(packet));
        Check(WorldChatStore.Ready && packet.Length == 44, "policy-only Java snapshot not routed");
        Check(WorldChatStore.GemFee == 10 && WorldChatStore.GoldFee == 10000000L, "fee wire mismatch");
        Check(WorldChatStore.MaxCharacters == 100 && WorldChatStore.CooldownMs == 5000, "limits wire mismatch");
        Check(WorldChatStore.Ticker.Current == null && WorldChatStore.Ticker.WaitingCount == 0, "snapshot replayed past text");
        byte[] utfPacket = File.ReadAllBytes(args[1]);
        Check(WorldChatProtocol.ReadText(new myReader(mSystem.convertToSbyte(utfPacket))) == "Xin chào 😀", "Java modified UTF decode");
        var writer = new myWriter(); WorldChatProtocol.WriteText(writer, "Xin chào 😀");
        sbyte[] encoded = writer.getData(); byte[] actual = new byte[encoded.Length]; Buffer.BlockCopy(encoded, 0, actual, 0, encoded.Length);
        Check(Convert.ToBase64String(actual) == Convert.ToBase64String(utfPacket), "client encoder differs from Java writeUTF");
        Check(WorldChatStore.FeeLabel.Contains("10 gem") && WorldChatStore.FeeLabel.Contains("10.000.000"), "fee label");
        Check(NotificationStore.UnreadCount == 1 && NotificationStore.Server.Count == 1, "world changed notification badge");
        var malformed = (byte[])packet.Clone(); malformed[18] = 0; malformed[19] = 0; malformed[20] = 0; malformed[21] = 0;
        NotificationProtocol.Read(new Message(malformed));
        Check(WorldChatStore.Ready && WorldChatStore.GemFee == 10, "malformed policy partially applied");
        Check(Service.gI().worldSnapshots == 1 && Service.gI().snapshots == 0, "malformed world packet requested wrong snapshot");
        byte[] live = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(args[0]), "world-chat-message.bin"));
        live[9] = 36; live[25] = 36; Buffer.BlockCopy(packet, 10, live, 10, 8); NotificationProtocol.Read(new Message(live));
        WorldChatStore.Ticker.Update(100, t => t.Length);
        Check(WorldChatStore.Ticker.Current.EndsWith("Tin số 35 😀"), "live UTF/text not queued");
        Check(!WorldChatStore.Receive(Entry(36), 36, WorldChatStore.Now), "duplicate requeued");
        Check(WorldChatStore.Receive(Entry(38), 38, WorldChatStore.Now), "live gap should not require removed history");
        NotificationProtocol.Read(new Message(packet));
        Check(WorldChatStore.Ticker.WaitingCount == 1, "old snapshot modified ticker");
        Check(WorldChatStore.Validate("  ") != null && WorldChatStore.Validate("\u00a0") != null, "blank accepted");
        Check(WorldChatStore.Validate(new string('x', 101)) != null, "over limit accepted");
        Check(WorldChatStore.Validate("line\nline") != null, "control accepted");
        Check(WorldChatStore.Validate(" Xin chào ") == null, "valid text rejected");
        Check(WorldChatStore.CharacterCount("😀😀") == 2, "Unicode counting");
        long id = WorldChatStore.StartRequest();
        Check(id > 0 && WorldChatStore.StartRequest() == 0, "duplicate send enabled");
        WorldChatStore.Result(id + 1, true, "stale", 7000, 2000);
        Check(WorldChatStore.PendingId == id, "unmatched result cleared pending");
        WorldChatStore.Result(id, true, "ok", 7000, 2000);
        Check(WorldChatStore.PendingId == 0 && WorldChatStore.ResultAccepted && WorldChatStore.StartRequest() == 0, "result/cooldown");
        WorldChatStore.Snapshot(38, 8000, 17, 123456, 80, 7000, 8000);
        Check(WorldChatStore.GemFee == 17 && WorldChatStore.GoldFee == 123456 && WorldChatStore.MaxCharacters == 80, "hardcoded policy");
        var composer = new WorldChatComposer(); int normal = 0, confirms = 0; string question = null;
        Action<string> normalChat = t => { if (t.Contains("admin")) normal++; };
        Action<string> popup = q => { confirms++; question = q; };
        composer.Select(false); composer.Submit("admin local", normalChat, popup);
        Check(normal == 1 && confirms == 0, "normal callback/S13 changed");
        composer.Select(true); Check(composer.Submit("admin world", normalChat, popup) == null, "world submit failed");
        Check(normal == 1 && confirms == 1 && question.Contains("17 gem") && question.Contains("123.456"), "world routed to normal/admin or fixed fee");
        composer.Decline(); Check(composer.Draft == "admin world" && !composer.Confirming && WorldChatStore.PendingId == 0, "decline cleared draft/sent");
        composer.Submit(composer.Draft, normalChat, popup); string error; id = composer.Agree(out error);
        Check(id > 0 && error == null && normal == 1, "agree did not start isolated world request");
        Check(composer.Agree(out error) == 0, "double agree sent twice");
        WorldChatStore.SendFailed(id); Check(WorldChatStore.PendingId == 0, "failed send locked input");
        composer.Submit("change policy", normalChat, popup);
        WorldChatStore.Snapshot(38, 8000, 18, 123456, 80, 7000, 8000);
        Check(composer.Agree(out error) == 0 && error != null, "fee changed after consent without re-confirmation");
        Check(composer.Submit(" ", normalChat, popup) != null, "blank confirmation");
        id = WorldChatStore.StartRequest(); typeof(WorldChatStore).GetField("pendingSince", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0L);
        Check(WorldChatStore.CheckTimeout() && WorldChatStore.PendingId == 0 && !WorldChatStore.ResultAccepted, "timeout not recovered");
        Check(WorldChatStore.ResultText.Contains("không tự gửi lại"), "timeout encourages duplicate charge");
        var ticker = new WorldChatTicker(); ticker.Enqueue("active"); ticker.Update(10, t => 4);
        Check(ticker.Current == "active" && ticker.X == 10, "stock right-edge start");
        ticker.Enqueue("drop"); ticker.Enqueue("second"); ticker.Enqueue("third"); ticker.Enqueue("fourth");
        Check(ticker.WaitingCount == 3 && ticker.Current == "active", "waiting cap removed running message");
        ticker.Update(10, t => 4); Check(ticker.X == 8, "stock 2px speed");
        for (int i=0;i<7;i++) ticker.Update(10, t => 4);
        Check(ticker.Current == null, "message did not vanish after one pass");
        foreach (string expected in new[] { "second", "third", "fourth" }) {
            ticker.Update(10, t => 4); Check(ticker.Current == expected, "FIFO/drop-oldest pending order");
            for (int i=0;i<8;i++) ticker.Update(10, t => 4);
            Check(ticker.Current == null, "queued message repeated/stopped");
        }
        for (int i=0;i<30;i++) ticker.Update(10, t => 4);
        Check(ticker.Current == null && ticker.WaitingCount == 0, "ticker repeated after drain");
        ticker.Enqueue("new"); ticker.Reset(); Check(ticker.WaitingCount == 0 && ticker.Current == null, "ticker reset");
        WorldChatStore.BeginSession();
        Check(!WorldChatStore.Ready && WorldChatStore.Ticker.Current == null && WorldChatStore.Ticker.WaitingCount == 0 && WorldChatStore.PendingId == 0, "session leaked policy/pending/queue");
        Console.WriteLine("PASS: " + assertions + " world-chat assertions; Java policy/live wire, UTF, fees, isolated compose/consent, ticker queue and session reset");
    }
}
