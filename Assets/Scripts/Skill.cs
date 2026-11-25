public class Skill
{
    public static bool isDisabled(int templateId)
    {
        return templateId == 24 || templateId == 25 || templateId == 26;
    }

    public SkillTemplate template;

    public short skillId;

    public int point;

    public long powRequire;

    public int coolDown;
    private int serverCooldown, serverManaUse;
    private bool hasServerValues;
    internal int EffectiveCooldown => hasServerValues ? serverCooldown : coolDown;
    internal void RememberServerValues() { serverCooldown = coolDown; serverManaUse = manaUse; hasServerValues = true; }
    internal void SynchronizeCooldown(int value) { serverCooldown = coolDown = System.Math.Max(0, value); hasServerValues = true; }
    internal void RestoreServerValues() { if (hasServerValues) { coolDown = serverCooldown; manaUse = serverManaUse; } }


    public long lastTimeUseThisSkill;

    public int dx;

    public int dy;

    public int maxFight;

    public int manaUse;

    public SkillOption[] options;

    public bool paintCanNotUseSkill;

    public short damage;

    public string moreInfo;

    public short price;

    public short curExp;

    public string strCurExp()
    {
        if (curExp / 10 >= 100)
        {
            return "MAX";
        }
        if (curExp % 10 == 0)
        {
            return curExp / 10 + "%";
        }
        int num = curExp % 10;
        return curExp / 10 + "." + num % 10 + "%";
    }

    public string strTimeReplay()
    {
        if (coolDown % 1000 == 0)
        {
            return coolDown / 1000 + string.Empty;
        }
        int num = coolDown % 1000;
        return coolDown / 1000 + "." + ((num % 100 != 0) ? (num / 10) : (num / 100));
    }

    public void paint(int x, int y, mGraphics g)
    {
        SmallImage.drawSmallImage(g, template.iconId, x, y, 0, StaticObj.VCENTER_HCENTER);
        long num = mSystem.currentTimeMillis();
        long num2 = num - lastTimeUseThisSkill;
        if (num2 < coolDown)
        {
            g.setColor(2721889, 0.7f);
            if (paintCanNotUseSkill && GameCanvas.gameTick % 6 > 2)
            {
                g.setColor(876862);
            }
            int num3 = (int)(num2 * 20 / coolDown);
            g.fillRect(x - 10, y - 10 + num3, 20, 20 - num3);
        }
        else
        {
            paintCanNotUseSkill = false;
        }
    }
}

// Confirm basic attacks and support casts; anchor basic cadence to send time.
internal static class AttackSync
{
    internal const byte Result = 9;
    private const long ReplyTimeoutMs = 2000;
    internal const int InputBufferMs = 50;
    private static Skill queuedSkill;
    private static SkillPaint queuedPaint;
    private static Mob queuedMob;
    private static Char queuedPlayer, queuedOwner;
    private static int queuedType, queuedMap, queuedZone;
    private static long nextRequest, pending, sentAt;
    private static Skill skill;
    private static int sentMap, sentZone;
    private static SkillPaint stagedPaint, paint;
    private static int stagedType, paintType;
    internal static bool IsBasicAttack(Skill value) => value != null && value.template != null
        && value.template.type == 1 && ((value.template.id >= 0 && value.template.id <= 5)
            || value.template.id == 9 || value.template.id == 17);
    private static bool IsAcknowledged(Skill value) => IsBasicAttack(value) || value?.template?.id == 7;
    internal static void Reset()
    {
        pending = 0; skill = queuedSkill = null;
        stagedPaint = paint = queuedPaint = null;
        queuedOwner = queuedPlayer = null;
        queuedMob = null;
    }
    private static bool Ready(Skill value)
    {
        long now = mSystem.currentTimeMillis();
        if (pending != 0 && now - sentAt >= ReplyTimeoutMs) {
            // Expire only the in-flight request; the separately validated input buffer remains intact.
            pending = 0; skill = null; paint = null;
        }
        return pending == 0 && now - value.lastTimeUseThisSkill >= value.EffectiveCooldown;
    }
    internal static bool Start(Char owner, SkillPaint requested, int type)
    {
        if (!IsAcknowledged(owner.myskill)) return false;
        if (ComboOverload.Block(owner.myskill)) return true;
        owner.ClearInvalidAttackFocus();
        if (owner.mobFocus == null && owner.charFocus == null || owner.charFocus != null && owner.myskill.template.id != 7 && !GameScr.IsLivePlayerTarget(owner.charFocus)) return true;
        if (owner.mobFocus != null && !GameScr.gI().isMeCanAttackMob(owner.mobFocus)) return true;
        if (!Ready(owner.myskill)) {
            long remaining = owner.myskill.EffectiveCooldown - (mSystem.currentTimeMillis() - owner.myskill.lastTimeUseThisSkill);
            if (remaining > 0 && remaining <= InputBufferMs && queuedSkill == null) {
                queuedOwner = owner; queuedSkill = owner.myskill; queuedPaint = requested; queuedType = type;
                queuedMob = owner.mobFocus; queuedPlayer = owner.charFocus;
                queuedMap = TileMap.mapID; queuedZone = TileMap.zoneID;
            }
            return true;
        }
        queuedSkill = null;
        stagedPaint = requested; stagedType = type;
        try {
            MyVector mobs = new MyVector(), players = new MyVector();
            if (owner.charFocus != null) players.addElement(owner.charFocus); else mobs.addElement(owner.mobFocus);
            Service.gI().sendPlayerAttack(mobs, players, players.size() > 0 ? 2 : 1);
        } finally { stagedPaint = null; }
        return true;
    }
    internal static void Update()
    {
        Char owner = Char.ExistingLocalPlayer;
        if (owner == null) { Reset(); return; }
        if (queuedSkill != null) {
            if (owner != queuedOwner || owner.myskill != queuedSkill || TileMap.mapID != queuedMap
                || TileMap.zoneID != queuedZone || owner.mobFocus != queuedMob || owner.charFocus != queuedPlayer
                || owner.ClearInvalidAttackFocus()) queuedSkill = null;
            else if (Ready(queuedSkill)) {
                SkillPaint next = queuedPaint; int type = queuedType;
                queuedSkill = null;
                owner.setSkillPaint(next, type);
            }
        }
        if (IsBasicAttack(owner.myskill) && (GameCanvas.AttackHeld && (Hud.CombatPcHud.CanInput || Hud.CombatMobileHud.CanInput)
            || Hud.CombatPcHud.CanInput && Hud.CombatPcPointer.Held(Hud.CombatPcHud.Attack)
            || Hud.CombatMobileHud.CanInput && Hud.MobileCombatPointers.Held(Hud.CombatMobileHud.Attack)))
            GameScr.gI().FireAttackInput(held: true);
    }

    // Called by the real transport for manual input and all auto-attack senders.
    internal static long BeforeSend()
    {
        Char owner = Char.myCharz();
        if (ComboOverload.Block(owner.myskill)) return -1;
        if (!IsAcknowledged(owner.myskill)) return 0;
        owner.ClearInvalidAttackFocus();
        if (owner.mobFocus == null && owner.charFocus == null || owner.charFocus != null && owner.myskill.template.id != 7 && !GameScr.IsLivePlayerTarget(owner.charFocus) || !Ready(owner.myskill)) return -1;
        if (owner.mobFocus != null && !GameScr.gI().isMeCanAttackMob(owner.mobFocus)) return -1;
        skill = owner.myskill; sentAt = mSystem.currentTimeMillis();
        pending = ++nextRequest;
        if (skill.template.id != 7) skill.lastTimeUseThisSkill = sentAt;
        sentMap = TileMap.mapID; sentZone = TileMap.zoneID;
        paint = stagedPaint ?? GameScr.sks[skill.skillId];
        paintType = stagedPaint != null ? stagedType : (TileMap.tileTypeAtPixel(owner.cx, owner.cy) & 2) != 0 ? 0 : 1;
        return pending;
    }
    internal static void SendFailed(long request) { if (request == pending) Reset(); }
    internal static bool TryRead(Message message)
    {
        var reader = message.reader(); reader.mark(0);
        if (reader.available() < 2 || reader.readByte() != Notifications.NotificationProtocol.Version || reader.readByte() != Result) {
            reader.reset(); return false;
        }
        try {
            long request = reader.readLong(); int id = reader.readShort(); bool accepted = reader.readBoolean();
            int remaining = reader.readInt(), cooldown = reader.readInt();
            if (request != pending || skill == null) return true;
            if (skill.skillId != id) { Reset(); return true; }
            Skill confirmedSkill = skill; SkillPaint confirmedPaint = paint; int confirmedType = paintType;
            confirmedSkill.SynchronizeCooldown(cooldown);
            pending = 0; skill = null; paint = null;
            if (accepted) {
                if (confirmedSkill.template.id == 7)
                    confirmedSkill.lastTimeUseThisSkill = mSystem.currentTimeMillis() - cooldown + System.Math.Max(0, remaining);
                // The ACK describes server time at send, not at receipt. Adding receive time
                // here adds network delay on every attack. Keep the original send timestamp.
                Char owner = Char.myCharz();
                // Injury/death arrives before this ACK. A dead target invalidates future input,
                // but must not suppress the animation of the hit the server just accepted.
                if (owner.myskill == confirmedSkill && TileMap.mapID == sentMap && TileMap.zoneID == sentZone
                    && owner.cHP > 0 && owner.statusMe != 5 && owner.statusMe != 14)
                    owner.ConfirmAttack(confirmedPaint, confirmedType);
            }
            else {
                confirmedSkill.lastTimeUseThisSkill = mSystem.currentTimeMillis() - cooldown + System.Math.Max(0, remaining);
                confirmedSkill.paintCanNotUseSkill = remaining > 0;
                if (remaining == 0 && confirmedSkill.template.id != 7) GameScr.info1.addInfo("Không thể tấn công mục tiêu này.", 0);
            }
        } catch (System.Exception) { Reset(); }
        return true;
    }
}

// Server snapshot on -45/subtype 31. Extrapolation is visual only; the server gates every hit.
internal static class ComboOverload
{
    internal const int Subtype = 31;
    private const int Version = 1;
    private static long sequence, received, lastNotice;
    private static double heat, gain, decay, growSeconds, decayAfterSeconds;
    private static bool locked;
    internal static int KaiokenHpPercent { get; private set; }
    internal static void Reset()
    {
        sequence = received = lastNotice = 0;
        heat = gain = decay = growSeconds = decayAfterSeconds = 0;
        locked = false; KaiokenHpPercent = 0;
    }
    internal static void Read(myReader reader)
    {
        if (reader.readUnsignedByte() != Version) throw new System.InvalidOperationException("Skill balance version");
        int hpPercent = reader.readUnsignedByte();
        long next = reader.readLong();
        int value = reader.readShort(); bool blocked = reader.readBoolean();
        int rise = reader.readShort(), fall = reader.readShort();
        int grow = reader.readInt(), grace = reader.readInt();
        if (reader.available() != 0 || value < 0 || value > 1000 || rise < 0 || fall < 0 || grow < 0 || grace < 0)
            throw new System.InvalidOperationException("Skill balance packet");
        if (next <= sequence) return;
        sequence = next; received = Hud.BuffIconStore.Now;
        heat = value; locked = blocked; gain = rise; decay = fall;
        growSeconds = grow / 1000.0; decayAfterSeconds = grace / 1000.0;
        KaiokenHpPercent = hpPercent;
    }
    private static double Heat
    {
        get {
            if (sequence == 0 || Char.ExistingLocalPlayer == null || Char.ExistingLocalPlayer.cHP <= 0) return 0;
            double seconds = System.Math.Max(0, Hud.BuffIconStore.Now - received) / 1000.0;
            double value = locked ? heat - decay * seconds
                : heat + gain * System.Math.Min(seconds, growSeconds) - decay * System.Math.Max(0, seconds - decayAfterSeconds);
            return System.Math.Max(0, System.Math.Min(1000, value));
        }
    }
    internal static bool Block(Skill skill)
    {
        if (skill == null) return false;
        string reason = null;
        Char owner = Char.ExistingLocalPlayer;
        if (skill.template.id == 17 && locked) reason = "Liên hoàn quá tải, chờ thanh về 0.";
        else if (skill.template.id == 9 && owner != null) {
            if (KaiokenHpPercent > 0 && owner.cHP <= owner.cHPFull * KaiokenHpPercent / 100)
                reason = "Không đủ HP để dùng Kaioken.";
            else if (skill.template.manaUseType == 1 && owner.cMP < owner.cMPFull * skill.manaUse / 100)
                reason = mResources.NOT_ENOUGH_MP;
        }
        if (reason == null) return false;
        long now = Hud.BuffIconStore.Now;
        if (now - lastNotice >= 1500) {
            lastNotice = now;
            GameScr.info1.addInfo(reason, 0);
        }
        return true;
    }
    internal static int Remaining => (int)System.Math.Floor(System.Math.Max(0, 100 - Heat / 10));
    internal static bool Locked => locked;
    internal static void Paint(mGraphics g, UnityEngine.Rect rect, Skill skill)
    {
        if (skill != null && skill.template.id == 17 && locked) Hud.BuffIconsHud.PaintInactive(rect, true);
    }
}
