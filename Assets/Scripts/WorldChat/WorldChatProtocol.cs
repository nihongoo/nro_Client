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
                    long id = reader.readLong(); bool accepted = reader.readBoolean(); string text = ReadText(reader);
                    long nextAllowedAt = reader.readLong();
                    if (!WorldChatStore.Result(id, accepted, text, nextAllowedAt, sequence, time)) RequestSnapshot();
                }
                else throw new InvalidOperationException("World chat action");
            }
            catch (Exception) { RequestSnapshot(); }
        }
        private static WorldChatEntry ReadEntry(myReader reader, int max)
        {
            var entry = new WorldChatEntry { Id = reader.readLong(), SentAt = reader.readLong(), SenderId = reader.readInt(), Sender = ReadText(reader), Text = ReadText(reader) };
            if (entry.Id <= 0 || entry.Sender.Length > 100 || WorldChatStore.CharacterCount(entry.Text) > max || string.IsNullOrWhiteSpace(entry.Text))
                throw new InvalidOperationException("World chat entry");
            return entry;
        }

        // Java DataInput/OutputStream use modified UTF-8 (UTF-16 surrogates encoded separately).
        // Keep this codec local to world chat; the game's legacy UTF helpers use standard UTF-8.
        public static void WriteText(myWriter writer, string text)
        {
            int length = 0;
            foreach (char c in text) length += c >= 1 && c <= 127 ? 1 : c <= 2047 ? 2 : 3;
            if (length > 65535) throw new InvalidOperationException("World chat UTF length");
            writer.writeShort(length);
            foreach (char c in text)
            {
                if (c >= 1 && c <= 127) writer.writeByte(c);
                else if (c <= 2047) { writer.writeByte(0xC0 | c >> 6); writer.writeByte(0x80 | c & 63); }
                else { writer.writeByte(0xE0 | c >> 12); writer.writeByte(0x80 | c >> 6 & 63); writer.writeByte(0x80 | c & 63); }
            }
        }
        public static string ReadText(myReader reader)
        {
            int length = reader.readUnsignedShort();
            if (length > 8192 || length > reader.available()) throw new InvalidOperationException("World chat UTF length");
            var chars = new char[length]; int count = 0, consumed = 0;
            while (consumed < length)
            {
                int c = reader.readByte() & 255; consumed++;
                if (c <= 127) chars[count++] = (char)c;
                else if ((c & 0xE0) == 0xC0 && consumed < length)
                {
                    int next = Continuation(reader); consumed++;
                    chars[count++] = (char)((c & 31) << 6 | next);
                }
                else if ((c & 0xF0) == 0xE0 && consumed + 1 < length)
                {
                    int next = Continuation(reader), last = Continuation(reader); consumed += 2;
                    chars[count++] = (char)((c & 15) << 12 | next << 6 | last);
                }
                else throw new InvalidOperationException("World chat UTF sequence");
            }
            return new string(chars, 0, count);
        }
        private static int Continuation(myReader reader)
        {
            int value = reader.readByte() & 255;
            if ((value & 0xC0) != 0x80) throw new InvalidOperationException("World chat UTF continuation");
            return value & 63;
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
