using System;

// -45/subtype 30. The server owns cast identity, targets, deadlines and cancellation.
public static class UltiCastClient
{
    public const int Subtype = 30, Version = 1;
    public const int Start = 0, Ready = 1, Finish = 2, Cancel = 3, Legacy = 4, Done = 5;
    public struct Packet
    {
        public int Action, Skill, TargetKind, Target, Duration, Lock;
        public long Request, Cast;
    }
    public static Packet Read(myReader r)
    {
        if (r.readUnsignedByte() != Version) throw new InvalidOperationException("Ulti version");
        Packet p = new Packet { Action = r.readUnsignedByte(), Skill = r.readUnsignedByte(), Request = r.readLong(),
            Cast = r.readLong(), TargetKind = r.readUnsignedByte(), Target = r.readInt(), Duration = r.readInt(), Lock = r.readInt() };
        if (r.available() != 0) throw new InvalidOperationException("Ulti packet length");
        return p;
    }
    public static void Write(myWriter w, Packet p)
    {
        w.writeByte(Version); w.writeByte(p.Action); w.writeByte(p.Skill); w.writeLong(p.Request); w.writeLong(p.Cast);
        w.writeByte(p.TargetKind); w.writeInt(p.Target); w.writeInt(p.Duration); w.writeInt(p.Lock);
    }
    private static long sequence, began, acknowledged;
    private static Packet current;
    private static bool sent, legacy;
    private static int map, zone;
    private static Char characterTarget;
    private static Mob mobTarget;
    public static bool OwnsCharge { get { return current.Request != 0 && current.Cast != 0 && !legacy; } }
    public static bool IsUlti(int skill) { return skill == 10 || skill == 11 || skill == 14; }
    public static void Reset() { current = new Packet(); sent = legacy = false; characterTarget = null; mobTarget = null; }
    private static void Send(int action)
    {
        Packet p = current; p.Action = action;
        Service.gI().sendUltiCast(p);
    }
    public static bool InterceptStatus(int status)
    {
        Char c = Char.ExistingLocalPlayer;
        if (c == null || c.myskill == null || !IsUlti(c.myskill.template.id) || (status != 4 && status != 7)) return false;
        if (current.Request != 0) {
            if (legacy) { if (status == 7 && !c.isStandAndCharge) Reset(); return false; }
            if (status == 7 && current.Cast != 0) FinishOnce();
            return true;
        }
        current = new Packet { Skill = c.myskill.template.id, Request = ++sequence };
        if (current.Skill != 14) {
            characterTarget = c.charFocus; mobTarget = c.mobFocus;
            if (characterTarget != null) { current.TargetKind = 1; current.Target = characterTarget.charID; }
            else if (mobTarget != null) { current.TargetKind = mobTarget.isMobMe ? 3 : 2; current.Target = mobTarget.mobId; }
        }
        map = TileMap.mapID; zone = TileMap.zoneID; began = Hud.BuffIconStore.Now;
        acknowledged = 0; sent = legacy = false;
        Send(Start); return true;
    }
    public static bool InterceptAttack()
    {
        Char c = Char.ExistingLocalPlayer;
        if (c == null || c.myskill == null || !IsUlti(c.myskill.template.id)) return false;
        if (legacy) { Reset(); return false; }
        FinishOnce(); return true;
    }
    private static void FinishOnce()
    {
        if (current.Request == 0 || current.Cast == 0 || sent) return;
        sent = true; Send(Finish);
    }
    public static void Receive(Packet p)
    {
        if (current.Request == 0 || p.Request != current.Request || p.Skill != current.Skill) return;
        Char c = Char.ExistingLocalPlayer;
        if (c == null) { Reset(); return; }
        if (p.Action == Ready) {
            if (p.Cast <= 0 || current.Cast != 0 || sent) return;
            current.Cast = p.Cast; current.Duration = p.Duration;
            acknowledged = Hud.BuffIconStore.Now;
            c.seconds = p.Duration; c.last = mSystem.currentTimeMillis();
        } else if (p.Action == Legacy) {
            legacy = true;
        } else if (p.Action == Cancel || p.Action == Done) {
            if (p.Cast != 0 && current.Cast != 0 && p.Cast != current.Cast) return;
            for (int i = 0; i < c.vSkill.size(); i++) {
                Skill skill = (Skill)c.vSkill.elementAt(i);
                if (skill != null && skill.template.id == p.Skill)
                    skill.lastTimeUseThisSkill = mSystem.currentTimeMillis() - (p.Action == Cancel ? skill.coolDown - p.Lock : 0);
            }
            c.stopUseChargeSkill(); Reset();
        }
    }
    public static void Update()
    {
        if (current.Request == 0) return;
        Char c = Char.ExistingLocalPlayer;
        bool lost = c == null || c.myskill == null || c.myskill.template.id != current.Skill
            || c.statusMe == 5 || c.statusMe == 14 || TileMap.mapID != map || TileMap.zoneID != zone;
        if (!lost && current.Skill != 14) {
            lost = current.TargetKind == 1
                ? c.charFocus != characterTarget || characterTarget == null || characterTarget.cHP <= 0
                : c.mobFocus != mobTarget || mobTarget == null || mobTarget.status == 0 || mobTarget.status == 1;
        }
        if (lost) {
            if (legacy) { Reset(); return; }
            if (current.Cast != 0) Send(Cancel);
            if (c != null) c.stopUseChargeSkill();
            // Keep the pending identity until the response: a late READY must be cancelled too.
            if (current.Cast != 0 || Hud.BuffIconStore.Now - began > 15000) Reset();
            return;
        }
        if (legacy) return;
        long now = Hud.BuffIconStore.Now;
        // Bomb must finish despite stun/stone/sleep stopping the normal Char animation update.
        if (current.Skill == 14 && acknowledged != 0 && now - acknowledged >= current.Duration) {
            FinishOnce(); c.stopUseChargeSkill();
        }
        if (now - began > 15000) {
            if (current.Cast != 0) Send(Cancel);
            c.stopUseChargeSkill(); Reset();
        }
    }
}
