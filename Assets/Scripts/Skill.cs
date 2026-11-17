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

// Basic targeted attacks wait for acceptance; server cooldowns and hit/miss rules remain authoritative.
internal static class AttackSync
{
    internal const byte Result = 9;
    private const long ReplyTimeoutMs = 2000;
    private static long nextRequest, pending, sentAt;
    private static Skill skill;
    private static SkillPaint stagedPaint, paint;
    private static int stagedType, paintType;
    internal static bool IsBasicAttack(Skill value) => value != null && value.template != null
        && value.template.type == 1 && value.template.id >= 0 && value.template.id <= 5;
    internal static void Reset() { pending = 0; skill = null; stagedPaint = paint = null; }
    private static bool Ready(Skill value)
    {
        long now = mSystem.currentTimeMillis();
        if (pending != 0 && now - sentAt >= ReplyTimeoutMs) Reset();
        return pending == 0 && now - value.lastTimeUseThisSkill >= value.EffectiveCooldown;
    }
    internal static bool Start(Char owner, SkillPaint requested, int type)
    {
        if (!IsBasicAttack(owner.myskill) || owner.charFocus == null && owner.mobFocus != null && owner.mobFocus.isMobMe) return false;
        if (!Ready(owner.myskill) || owner.mobFocus == null && owner.charFocus == null) return true;
        stagedPaint = requested; stagedType = type;
        try {
            MyVector mobs = new MyVector(), players = new MyVector();
            if (owner.charFocus != null) players.addElement(owner.charFocus); else mobs.addElement(owner.mobFocus);
            Service.gI().sendPlayerAttack(mobs, players, players.size() > 0 ? 2 : 1);
        } finally { stagedPaint = null; }
        return true;
    }
    // Called by the real transport for manual input and all auto-attack senders.
    internal static long BeforeSend()
    {
        Char owner = Char.myCharz();
        if (!IsBasicAttack(owner.myskill) || owner.charFocus == null && owner.mobFocus != null && owner.mobFocus.isMobMe) return 0;
        if (!Ready(owner.myskill)) return -1;
        skill = owner.myskill; sentAt = mSystem.currentTimeMillis();
        pending = ++nextRequest; skill.lastTimeUseThisSkill = sentAt;
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
                // Preserve the configured cadence and any later ready time reported after server processing.
                confirmedSkill.lastTimeUseThisSkill = System.Math.Max(confirmedSkill.lastTimeUseThisSkill,
                    mSystem.currentTimeMillis() + System.Math.Max(0, remaining) - cooldown);
                if (Char.myCharz().myskill == confirmedSkill) Char.myCharz().ConfirmAttack(confirmedPaint, confirmedType);
            }
            else {
                confirmedSkill.lastTimeUseThisSkill = mSystem.currentTimeMillis() - cooldown + System.Math.Max(0, remaining);
                confirmedSkill.paintCanNotUseSkill = remaining > 0;
                if (remaining == 0) GameScr.info1.addInfo("Không thể tấn công mục tiêu này.", 0);
            }
        } catch (System.Exception) { Reset(); }
        return true;
    }
}
