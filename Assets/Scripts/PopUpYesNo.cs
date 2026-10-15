using Notifications;
using UnityEngine;

// Quick view of the newest invitation. Older invitations remain in NotificationStore.
public class PopUpYesNo : IActionListener
{
    public readonly long InvitationId;
    public readonly Command cmdYes, cmdNo;
    private readonly string text;
    private int X, Y, bodyWidth;
    private const int W = 120, H = 29, ButtonSize = 32;
    private int remaining;
    private const string Bounds = "invitation.quick", Body = "invitation.quick.body",
        Yes = "invitation.quick.yes", No = "invitation.quick.no";
    private const int Gap = 3, RowGap = Hud.TopActionsHud.ButtonGap, MinimumTextWidth = 20;

    internal static void RegisterHudRects()
    {
        const int order = Hud.HudLayout.OverlayOrder + 1;
        System.Func<bool> visible = () => GameScr.instance != null && GameCanvas.currentScreen == GameScr.instance
            && !NotificationCenter.IsOpen && GameScr.instance.popUpYesNo != null && Hud.HudLayout.GetRect(Bounds).width > 0;
        Hud.HudLayout.Register(Bounds, QuickBounds, order, visible);
        Hud.HudLayout.Register(Body, () => {
            Rect bounds = Hud.HudLayout.GetRect(Bounds);
            if (bounds.width <= 0) return new Rect();
            return new Rect(bounds.x, bounds.y + (ButtonSize - H) / 2, bounds.width - (ButtonSize + Gap) * 2, H);
        }, order, visible, false);
        Hud.HudLayout.Register(Yes, () => {
            Rect bounds = Hud.HudLayout.GetRect(Bounds), body = Hud.HudLayout.GetRect(Body);
            return bounds.width <= 0 ? new Rect() : new Rect(body.xMax + Gap, bounds.y, ButtonSize, ButtonSize);
        }, order, visible);
        Hud.HudLayout.Register(No, () => {
            Rect bounds = Hud.HudLayout.GetRect(Bounds), yes = Hud.HudLayout.GetRect(Yes);
            return bounds.width <= 0 ? new Rect() : new Rect(yes.xMax + Gap, yes.y, ButtonSize, ButtonSize);
        }, order, visible);
    }

    private static Rect QuickBounds()
    {
        Rect hub = Hud.HudLayout.GetRect(Hud.TopActionsHud.Hub);
        float right = hub.xMin - RowGap;
        // The whole quick invitation, including Yes/No, stays in the right half.
        // On narrow screens its full content remains available in the bell's Invitations tab.
        float width = Mathf.Floor(Mathf.Min(W + (ButtonSize + Gap) * 2, right - Mathf.Ceil(GameCanvas.w / 2f)));
        if (width < MinimumTextWidth + (ButtonSize + Gap) * 2) return new Rect();
        return new Rect(right - width, hub.y + (hub.height - ButtonSize) / 2, width, H + 17);
    }

    public PopUpYesNo(NotificationEntry entry)
    {
        InvitationId = entry.Id; text = entry.Text;
        cmdYes = new Command("", this, 1, null) { w = ButtonSize, h = ButtonSize };
        cmdNo = new Command("", this, 2, null) { w = ButtonSize, h = ButtonSize };
        TextInfo.reset(); update();
    }

    private void UpdatePosition()
    {
        Rect body = Hud.HudLayout.GetRect(Body), yes = Hud.HudLayout.GetRect(Yes), no = Hud.HudLayout.GetRect(No);
        X = (int)body.x; Y = (int)body.y; bodyWidth = (int)body.width;
        cmdYes.x = (int)yes.x; cmdYes.y = (int)yes.y;
        cmdNo.x = (int)no.x; cmdNo.y = (int)no.y;
    }

    internal Rect VisualBounds {
        get { return Hud.HudLayout.GetRect(Bounds); }
    }

    public void update()
    {
        UpdatePosition();
        NotificationEntry entry = NotificationStore.FindInvitation(InvitationId);
        remaining = entry == null ? 0 : (int)System.Math.Max(0, (entry.ExpiresAt - NotificationStore.ServerNow + 999) / 1000);
    }

    // Run before panel/combat input: these two buttons are usable over other panels.
    public bool HandleInput()
    {
        if (NotificationCenter.ConsumesInput || !GameCanvas.isPointerJustRelease || !GameCanvas.isPointerClick) return false;
        Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
        string hit = Hud.HudLayout.HitTest(pointer);
        if (hit == Yes) cmdYes.performAction();
        else if (hit == No) cmdNo.performAction();
        else return false;
        GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
        return true;
    }

    public void paint(mGraphics g)
    {
        if (NotificationCenter.IsOpen || Hud.HudLayout.GetRect(Bounds).width <= 0) return;
        UpdatePosition();
        g.translate(-g.getTranslateX(), -(g.getTranslateY() - mGraphics.addYWhenOpenKeyBoard));
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        PopUp.paintPopUp(g, X, Y, bodyWidth, H, 16777215, false);
        TextInfo.paint(g, text, X + 5, Y + (H - mFont.tahoma_7.getHeight()) / 2,
            bodyWidth - 10, mFont.tahoma_7.getHeight() + 2, mFont.tahoma_7);
        PaintAction(g, Hud.HudLayout.GetRect(Yes), true);
        PaintAction(g, Hud.HudLayout.GetRect(No), false);
        mFont.tahoma_7_yellow.drawString(g, remaining + "s", X + bodyWidth / 2, Y + H + 2, 2, mFont.tahoma_7_grey);
    }

    // Sprite fonts lack Unicode check/cross glyphs. Draw the symbols in logical pixels.
    public static void PaintAction(mGraphics g, Rect bounds, bool accept)
    {
        int cx = (int)bounds.center.x, cy = (int)bounds.center.y;
        // Reuse the original round Yes button at its native size, centered in the touch target.
        g.drawImage(GameScr.imgNut, cx, cy, 3);
        g.setColor(accept ? 0x187A27 : 0xC02727);
        if (accept) { Stroke(g, cx - 6, cy - 1, cx - 2, cy + 3); Stroke(g, cx - 2, cy + 3, cx + 5, cy - 5); }
        else { Stroke(g, cx - 5, cy - 5, cx + 4, cy + 4); Stroke(g, cx + 4, cy - 5, cx - 5, cy + 4); }
    }

    private static void Stroke(mGraphics g, int x1, int y1, int x2, int y2)
    {
        int steps = System.Math.Max(System.Math.Abs(x2 - x1), System.Math.Abs(y2 - y1));
        for (int i = 0; i <= steps; i++) g.fillRect(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps, 2, 2);
    }

    public void perform(int action, object p) { NotificationStore.RespondToInvitation(InvitationId, action == 1); }
}
