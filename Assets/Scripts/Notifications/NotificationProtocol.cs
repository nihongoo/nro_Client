using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Notifications
{
    public static class NotificationProtocol
    {
        public const sbyte Command = -109;
        public const byte Version = 2, Upsert = 0, Remove = 1, Snapshot = 2, Respond = 3;
        private static long nextResync;

        public static void Read(Message message)
        {
            try
            {
                myReader reader = message.reader();
                if (reader.readByte() != Version) throw new InvalidOperationException("Notification version");
                int action = reader.readByte();
                long sequence = reader.readLong(), time = reader.readLong();
                if (action == Snapshot)
                {
                    int count = reader.readUnsignedShort(), bosses = 0, server = 0, invitations = 0;
                    if (count > 106) throw new InvalidOperationException("Notification count");
                    var entries = new List<NotificationEntry>(count);
                    var ids = new HashSet<long>();
                    for (int i = 0; i < count; i++)
                    {
                        NotificationEntry entry = ReadEntry(reader);
                        if (!ids.Add(entry.Id)) throw new InvalidOperationException("Notification ID");
                        if (entry.Kind == NotificationKind.Boss) bosses++;
                        else if (entry.Kind == NotificationKind.Server) server++; else invitations++;
                        entries.Add(entry);
                    }
                    if (bosses > 6 || server > 50 || invitations > 50) throw new InvalidOperationException("Notification limits");
                    NotificationStore.Snapshot(entries, sequence, time);
                }
                else if (action == Upsert)
                {
                    if (!NotificationStore.Upsert(ReadEntry(reader), sequence, time)) RequestResync();
                }
                else if (action == Remove)
                {
                    NotificationKind kind = ReadKind(reader);
                    NotificationStore.Remove(kind, reader.readLong(), sequence, time);
                }
                else throw new InvalidOperationException("Notification action");
            }
            catch (Exception)
            {
                // Keep the previous valid store if a packet is incomplete or unsupported.
                RequestResync();
            }
        }

        private static NotificationKind ReadKind(myReader reader)
        {
            int value = reader.readByte();
            if (value < 0 || value > 2) throw new InvalidOperationException("Notification kind");
            return (NotificationKind)value;
        }

        private static NotificationEntry ReadEntry(myReader reader)
        {
            var entry = new NotificationEntry { Id = reader.readLong(), Kind = ReadKind(reader), UpdatedAt = reader.readLong() };
            if (entry.Id <= 0) throw new InvalidOperationException("Notification ID");
            if (entry.Kind == NotificationKind.Boss)
            {
                entry.BossName = reader.readUTF(); entry.MapId = reader.readInt(); entry.MapName = reader.readUTF();
                entry.Killed = reader.readBoolean(); entry.Killer = reader.readUTF();
            }
            else if (entry.Kind == NotificationKind.Invitation)
            {
                entry.InvitationType = (byte)reader.readByte(); entry.SenderId = reader.readInt();
                entry.ExpiresAt = reader.readLong(); entry.Text = reader.readUTF();
                if (entry.InvitationType > 3 || entry.ExpiresAt <= entry.UpdatedAt)
                    throw new InvalidOperationException("Invitation payload");
            }
            else { entry.Category = reader.readUTF(); entry.Text = reader.readUTF(); }
            return entry;
        }

        private static void RequestResync()
        {
            long now = Stopwatch.GetTimestamp();
            if (now < nextResync) return;
            nextResync = now + Stopwatch.Frequency * 5;
            Service.gI().requestNotificationSnapshot();
        }
    }
}
