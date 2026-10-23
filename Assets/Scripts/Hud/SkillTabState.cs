using System;
using Mod.CuongLe;

namespace Hud
{
    internal static class SkillTabState
    {
        internal const sbyte CommandId = 104;
        internal static readonly sbyte[] Order = new sbyte[8];
        internal static bool Ready;
        internal static long FinishToken = -1, RemainingAtRead, Duration;
        internal static short LearningBook = -1;
        internal static int LearningTemplate = -1, LearningLevel, QuickPrice;
        private static long readAt, nextRequest, firstRequest = -1;
        internal static bool Waiting {get{return !Ready && firstRequest >= 0 && mSystem.currentTimeMillis()-firstRequest < 2000;}}
        private static SkillTemplate[] panelTemplates;
        private static NClass templateClass;
        private static int teacherMap = -1, teacherId;
        private static long navigationStarted, nextWalk;
        internal static long Remaining => System.Math.Max(0, RemainingAtRead - (mSystem.currentTimeMillis() - readAt));
        internal static bool Studying(int id) {return Ready && FinishToken != -1 && LearningTemplate == id;}
        internal static void Reset()
        {
            PotentialState.Reset();
            Ready = false; FinishToken = -1; LearningBook = -1; LearningTemplate = -1;
            teacherMap = -1; nextRequest = 0; firstRequest = -1; panelTemplates = null; templateClass = null;
            for (int i = 0; i < 8; i++) Order[i] = -1;
        }
        internal static void Read(Message message)
        {
            int action=message.reader().readByte();
            if(action==4||action==5){PotentialState.Read(action,message);return;}
            if(action!=0)return;
            long token = message.reader().readLong(), remaining = message.reader().readLong(), duration = message.reader().readLong();
            short book = message.reader().readShort(); int template = message.reader().readByte(), level = message.reader().readByte();
            int price = message.reader().readInt();
            sbyte[] order = new sbyte[8]; bool changed = !Ready;
            for (int i = 0; i < 8; i++) { order[i] = message.reader().readByte(); changed |= order[i] != Order[i]; }
            FinishToken = token; RemainingAtRead = remaining; Duration = duration; LearningBook = book;
            LearningTemplate = template; LearningLevel = level; QuickPrice = price; readAt = mSystem.currentTimeMillis();
            Array.Copy(order, Order, 8); Ready = true;
            if (changed) { panelTemplates = null; ApplyOrder(); }
            HudLayout.Invalidate();
        }
        internal static void ApplyOrder()
        {
            if (!Ready) return;
            GameScr.gI().onOSkill(Order); GameScr.gI().onKSkill(Order);
        }
        internal static void Request()
        {
            if(firstRequest<0)firstRequest=mSystem.currentTimeMillis();
            nextRequest = mSystem.currentTimeMillis() + 2000;
            Service.gI().skillTabRequest(0);
        }
        internal static void Swap(int id, int slot) => Service.gI().skillTabRequest(2, id, slot);
        internal static void UseBook(int id) => Service.gI().skillTabRequest(3, id);
        private sealed class QuickConfirmation : IActionListener
        {
            private readonly short book; private readonly long token; private readonly int price;
            internal QuickConfirmation() { book = LearningBook; token = FinishToken; price = QuickPrice; }
            public void perform(int action, object unused)
            {
                GameCanvas.endDlg();
                if (action == 1) Service.gI().skillTabRequest(1, book, price, token);
            }
        }
        internal static void ConfirmQuick()
        {
            if (!Ready || FinishToken == -1) return;
            var confirmation = new QuickConfirmation();
            GameCanvas.startYesNoDlg("Học nhanh cấp " + LearningLevel + " với " + QuickPrice + " ngọc?",
                new Command(mResources.YES, confirmation, 1, null), new Command(mResources.CANCEL, confirmation, 0, null));
        }
        internal static SkillTemplate[] Templates()
        {
            NClass currentClass = Char.myCharz().nClass;
            if (panelTemplates != null && ReferenceEquals(templateClass, currentClass)) return panelTemplates;
            SkillTemplate[] available = currentClass?.availableSkillTemplates ?? new SkillTemplate[0];

            var result = new SkillTemplate[System.Math.Min(8, available.Length)];
            Array.Copy(available,result,result.Length);
            templateClass = currentClass; panelTemplates = result;
            return result;
    }

        internal static int SlotFor(int templateId)
        {
            if(Ready)return Array.IndexOf(Order,(sbyte)templateId);
            Skill[] hud=Main.isPC?GameScr.keySkill:GameScr.onScreenSkill;
            if(hud!=null)for(int i=0;i<System.Math.Min(8,hud.Length);i++)if(hud[i]?.template?.id==templateId)return i;
            return -1;
        }
        internal static int BookId(int template, int level)
        {
            if (level < 1 || level > 7) return -1;
            if (template == 2) return level == 7 ? 86 : 78 + level;

            int[] ids = {0,1,3,4,5,6,7,8,9,10,11,12,13,14,17,18,19,20,21,22,23};
            int[] starts = {66,94,101,87,108,115,122,129,300,307,328,335,314,321,481,474,434,488,509,495,502};
            for (int i = 0; i < ids.Length; i++) if (ids[i] == template) return starts[i] + level - 1;
            return -1;
        }
        internal static int FindBook(int template, int level)
        {
            int id = BookId(template,level); Item[] bag = Char.myCharz().arrItemBag;
            if (id < 0 || bag == null) return -1;
            for (int i = 0; i < bag.Length; i++) if (bag[i]?.template != null && bag[i].template.id == id && bag[i].quantity > 0) return i;
            return -1;
    }
        internal static long StudyDuration(int level)
        {
            long[] times = {900000L,1800000L,3600000L,86400000L,259200000L,604800000L,1296000000L};
            return level >= 1 && level <= times.Length ? times[level - 1] : 0;
        }
        internal static string Clock(long milliseconds)
        {
            long seconds = System.Math.Max(0,milliseconds) / 1000;
            return (seconds / 3600).ToString("00") + ":" + (seconds / 60 % 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        internal static string TeacherName => Char.myCharz().cgender == 0 ? "Quy Lão" : Char.myCharz().cgender == 1 ? "Trưởng lão Guru" : "Vua Vegeta";
        internal static void GoToTeacher()
        {
            if (Char.myCharz().cHP <= 0) { GameCanvas.startOKDlg(mResources.can_not_do_when_die); return; }
            int gender = Char.myCharz().cgender;
            teacherMap = gender == 0 ? 5 : gender == 1 ? 13 : 20; teacherId = 13 + gender;
            navigationStarted = mSystem.currentTimeMillis(); nextWalk = 0;
            AutoMap.FinishXmap();
            if (TileMap.mapID != teacherMap) { AutoMap.IdMapEnd = teacherMap; AutoMap.isXmaping = true; }
            GameCanvas.panel.hide();
        }
        internal static void Update()
        {
            PotentialState.Update();
            if (GameCanvas.panel != null && GameCanvas.panel.SkillPanelActive && GameCanvas.panel.isShow
                && mSystem.currentTimeMillis() >= nextRequest) Request();
            if (teacherMap < 0) return;
            if (Char.myCharz().cHP <= 0 || mSystem.currentTimeMillis() - navigationStarted > 180000) { teacherMap = -1; return; }
            if (TileMap.mapID != teacherMap) {
                if (!AutoMap.isXmaping && mSystem.currentTimeMillis() - navigationStarted > 2000) teacherMap = -1;
                return;
            }
            Npc npc = GameScr.findNPCInMap((short)teacherId);
            if (npc == null || Char.isLoadingMap || Char.ischangingMap) return;
            Char c = Char.myCharz();
            if (System.Math.Abs(c.cx - npc.cx) <= 40 && System.Math.Abs(c.cy - npc.cy) <= 40) {
                c.currentMovePoint = null; c.npcFocus = npc; teacherMap = -1;
                Service.gI().openMenu(teacherId); return;
            }
            if (mSystem.currentTimeMillis() >= nextWalk) {
                c.mobFocus = null; c.charFocus = null; c.itemFocus = null; c.npcFocus = npc;
                c.currentMovePoint = new MovePoint(npc.cx > c.cx ? npc.cx - 24 : npc.cx + 24, npc.cy);
                c.endMovePointCommand = null; nextWalk = mSystem.currentTimeMillis() + 1500;
            }
        }
    }
}
