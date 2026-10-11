namespace Notifications
{
    internal static class NotificationSkin
    {
        private static Image bellIcon, closeIcon;
        private static int assetZoom = -1;
        internal static Image BellIcon { get { LoadIcons(); return bellIcon; } }
        internal static Image CloseIcon { get { LoadIcons(); return closeIcon; } }

        private static void LoadIcons()
        {
            if (assetZoom == mGraphics.zoomLevel && bellIcon != null && closeIcon != null) return;
            bellIcon = GameCanvas.loadImage("/mainImage/notification-bell.png");
            closeIcon = GameCanvas.loadImage("/mainImage/myTexture2dbtX.png");
            assetZoom = mGraphics.zoomLevel;
        }

        // Reuse the stock popup texture. Clamp the final edge tile so it cannot cover a corner.
        internal static void PaintFrame(mGraphics g, int x, int y, int w, int h, bool selected)
        {
            if (w < 20 || h < 20) return;
            Image skin = selected ? PopUp.imgPopUp2 : PopUp.imgPopUp;
            g.setColor(selected ? 12052656 : 16770503);
            g.fillRect(x + 10, y + 10, w - 20, h - 20);
            for (int offset = 10; offset < w - 10; offset += 10)
            {
                int length = System.Math.Min(10, w - 10 - offset);
                g.drawRegion(skin, 0, 10, length, 10, 0, x + offset, y, 0);
                g.drawRegion(skin, 0, 60, length, 10, 0, x + offset, y + h - 10, 0);
            }
            for (int offset = 10; offset < h - 10; offset += 10)
            {
                int length = System.Math.Min(10, h - 10 - offset);
                g.drawRegion(skin, 0, 30, 10, length, 0, x, y + offset, 0);
                g.drawRegion(skin, 0, 40, 10, length, 0, x + w - 10, y + offset, 0);
            }
            g.drawRegion(skin, 0, 0, 10, 10, 0, x, y, 0);
            g.drawRegion(skin, 0, 20, 10, 10, 0, x + w - 10, y, 0);
            g.drawRegion(skin, 0, 50, 10, 10, 0, x, y + h - 10, 0);
            g.drawRegion(skin, 0, 70, 10, 10, 0, x + w - 10, y + h - 10, 0);
        }
    }
}
