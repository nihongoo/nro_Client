using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WorldChat
{
    public static class WorldChatProtocol
    {
        public const byte Snapshot = 4, Append = 5, Submit = 6, Result = 7;
        private static long nextResync;
        public static void Read(int action, myReader reader)
        {
            try
            {
                long sequence = reader.readLong(), time = reader.readLong();
                if (sequence < 0) throw new InvalidOperationException("World chat sequence");
                if (action == Snapshot)
                {
                    int gems = reader.readInt(); long gold = reader.readLong();
                    int max = reader.readUnsignedShort(), cooldown = reader.readInt(), retention = reader.readByte();
                    long nextAllowedAt = reader.readLong(); int count = reader.readUnsignedShort();
                    if (gems <= 0 || gold <= 0 || max <= 0 || max > 1000 || cooldown <= 0 || retention <= 0 || retention > 100 || count > retention)
                        throw new InvalidOperationException("World chat policy/count");
                    var entries = new List<WorldChatEntry>(); long last = 0;
                    for (int i = 0; i < count; i++)
                    {
                        var entry = ReadEntry(reader, max);
                        if (entry.Id <= last || entry.Id > sequence) throw new InvalidOperationException("World chat order");
                        entries.Add(entry); last = entry.Id;
                    }
                    if (count > 0 && last != sequence) throw new InvalidOperationException("World chat tail");
                    WorldChatStore.Snapshot(entries, sequence, time, gems, gold, max, cooldown, retention, nextAllowedAt);
                }
                else if (action == Append)
                {
                    var entry = ReadEntry(reader, WorldChatStore.Ready ? WorldChatStore.MaxCharacters : 1000);
                    if (entry.Id != sequence) throw new InvalidOperationException("World chat ID");
                    if (!WorldChatStore.Append(entry, sequence, time)) RequestSnapshot();
                }
                else if (action == Result)
                {
                    long id = reader.readLong(); bool accepted = reader.readBoolean(); string text = reader.readUTF();
                    long nextAllowedAt = reader.readLong();
                    if (!WorldChatStore.Result(id, accepted, text, nextAllowedAt, sequence, time)) RequestSnapshot();
                }
                else throw new InvalidOperationException("World chat action");
            }
            catch (Exception) { RequestSnapshot(); }
        }
        private static WorldChatEntry ReadEntry(myReader reader, int max)
        {
            var entry = new WorldChatEntry { Id = reader.readLong(), SentAt = reader.readLong(), SenderId = reader.readInt(), Sender = reader.readUTF(), Text = reader.readUTF() };
            if (entry.Id <= 0 || entry.Sender.Length > 100 || WorldChatStore.CharacterCount(entry.Text) > max || string.IsNullOrWhiteSpace(entry.Text))
                throw new InvalidOperationException("World chat entry");
            return entry;
        }
        public static void RequestSnapshot()
        {
            long now = Stopwatch.GetTimestamp();
            if (now < nextResync) return;
            nextResync = now + Stopwatch.Frequency * 5;
            Service.gI().requestWorldChatSnapshot();
        }
    }
}
