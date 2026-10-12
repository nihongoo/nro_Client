using Notifications;
using UnityEngine;

// Quick view of the newest invitation. Older invitations remain in NotificationStore.
public class PopUpYesNo : IActionListener
{
    public readonly long InvitationId;
    public readonly Command cmdYes, cmdNo;
    private readonly string text;
    private int X, Y;
    private const int W = 120, H = 29, ButtonSize = 32;
    private int remaining;

    public PopUpYesNo(NotificationEntry entry)
    {
        InvitationId = entry.Id; text = entry.Text;
        cmdYes = new Command("", this, 1, null) { w = ButtonSize, h = ButtonSize };
        cmdNo = new Command("", this, 2, null) { w = ButtonSize, h = ButtonSize };
        TextInfo.reset(); update();
    }

    private void UpdatePosition()
    {
        X = GameCanvas.w - 5 - W; Y = 45;
        if (GameCanvas.w - 50 > 155 + W) { X = GameCanvas.w - 55 - W; Y = 5; }
        cmdYes.x = X - ButtonSize - 3; cmdNo.x = cmdYes.x - ButtonSize - 3;
        cmdYes.y = cmdNo.y = Y + (H - ButtonSize) / 2;
    }

    internal Rect VisualBounds {
        get { UpdatePosition(); return new Rect(cmdNo.x, cmdNo.y, X + W - cmdNo.x, Y + H + 17 - cmdNo.y); }
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
        if (!GameCanvas.isPointerJustRelease || !GameCanvas.isPointerClick) return false;
        Vector2 pointer = new Vector2(GameCanvas.px, GameCanvas.py);
        if (new Rect(cmdYes.x, cmdYes.y, ButtonSize, ButtonSize).Contains(pointer)) cmdYes.performAction();
        else if (new Rect(cmdNo.x, cmdNo.y, ButtonSize, ButtonSize).Contains(pointer)) cmdNo.performAction();
        else return false;
        GameCanvas.clearAllPointerEvent(); GameCanvas.clearKeyHold(); GameCanvas.clearKeyPressed();
        return true;
    }

    public void paint(mGraphics g)
    {
        UpdatePosition();
        g.translate(-g.getTranslateX(), -g.getTranslateY()); g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
        PopUp.paintPopUp(g, X, Y, W, H, 16777215, false);
        TextInfo.paint(g, text, X + 5, Y + H / 2 - 4, W - 10, H, mFont.tahoma_7);
        PaintAction(g, new Rect(cmdYes.x, cmdYes.y, ButtonSize, ButtonSize), true);
        PaintAction(g, new Rect(cmdNo.x, cmdNo.y, ButtonSize, ButtonSize), false);
        mFont.tahoma_7_yellow.drawString(g, remaining + "s", X + W / 2, Y + H + 2, 2, mFont.tahoma_7_grey);
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
