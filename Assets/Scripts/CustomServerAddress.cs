using System;
using System.Globalization;

public static class CustomServerAddress
{
    private const string RmsKey = "customServerEndpoint";
    private sealed class Endpoint
    {
        public readonly string Host;
        public readonly int Port;
        public Endpoint(string host, int port) { Host = host; Port = port; }
    }

    // One immutable snapshot keeps host and port paired across UI/network threads.
    private static volatile Endpoint custom;
    public static bool Enabled { get { return custom != null; } }
    // Only the UI thread owns the connection deadline and dialog state.
    private static bool connectionPending;
    private static int connectionStarted;
    private static string attemptedEndpoint;
    private static readonly ConnectionActions connectionActions = new ConnectionActions();

    private sealed class ConnectionActions : IActionListener
    {
        public void perform(int action, object unused)
        {
            if (action == 2) Reset();
            Reconnect();
        }
    }

    public static void Load()
    {
        string stored = Rms.loadRMSString(RmsKey);
        string host, error;
        int port;
        int separator = stored == null ? -1 : stored.LastIndexOf(':');
        custom = separator > 0 && Validate(stored.Substring(0, separator), stored.Substring(separator + 1), out host, out port, out error)
            ? new Endpoint(host, port) : null;
    }

    public static void Save(string host, int port)
    {
        string normalized, error;
        int validatedPort;
        if (!Validate(host, port.ToString(CultureInfo.InvariantCulture), out normalized, out validatedPort, out error))
            throw new ArgumentException(error);
        Rms.saveRMSString(RmsKey, normalized + ":" + validatedPort.ToString(CultureInfo.InvariantCulture));
        custom = new Endpoint(normalized, validatedPort);
    }

    public static void Reset()
    {
        Rms.deleteRecord(RmsKey);
        custom = null;
    }

    public static void Resolve(ref string host, ref int port)
    {
        Endpoint selected = custom;
        if (selected == null) return;
        host = selected.Host;
        port = selected.Port;
    }

    public static string Host
    {
        get { Endpoint selected = custom; return selected == null ? GameMidlet.IP : selected.Host; }
    }

    public static int Port
    {
        get { Endpoint selected = custom; return selected == null ? GameMidlet.PORT : selected.Port; }
    }

    public static string Label { get { return Host + ":" + Port.ToString(CultureInfo.InvariantCulture); } }

    public static void PaintLabel(mGraphics g, int y)
    {
        string text = "Server: " + Label;
        if (Session_ME.ConnectedViaFallback) text += " (đang dùng " + Session_ME.ConnectedEndpoint + ")";
        int width = GameCanvas.w - 12;
        // Scroll long hostnames so the full destination remains readable on a narrow screen.
        int overflow = mFont.tahoma_7_white.getWidth(text) - width;
        int offset = overflow > 0 ? (GameCanvas.gameTick / 3) % (overflow + 50) : 0;
        offset = System.Math.Min(offset, System.Math.Max(0, overflow));
        g.setClip(6, y, width, 15);
        mFont.tahoma_7_white.drawString(g, text, 6 - offset, y, 0, mFont.tahoma_7_grey);
        g.setClip(0, 0, GameCanvas.w, GameCanvas.h);
    }

    public static void ConnectionStarted()
    {
        if (!Enabled && !connectionPending) return;
        // Repeated connect requests must not extend an already running deadline.
        if (connectionPending && attemptedEndpoint == Label) return;
        attemptedEndpoint = Label;
        connectionStarted = Environment.TickCount;
        connectionPending = true;
    }

    public static void UpdateConnection()
    {
        if (!connectionPending || LoginScr.isLoggingIn || GameCanvas.currentDialog is CustomServerDialog) return;
        if (Session_ME.readyForLogin() && !Controller.isConnectOK
            && !Controller.isConnectionFail && !Controller.isDisconnected)
        {
            connectionPending = false;
            Char.isLoadingMap = false;
            if (GameCanvas.currentDialog == GameCanvas.msgdlg && GameCanvas.msgdlg.isWait) GameCanvas.endDlg();
            return;
        }
        if (unchecked(Environment.TickCount - connectionStarted) >= 15000)
        {
            CloseConnection();
            ShowConnectionError("Kết nối tới máy chủ quá lâu.");
        }
    }

    private static void CloseConnection()
    {
        connectionPending = false;
        // close() advances the existing network generation before any new endpoint is started.
        Session_ME.gI().close();
        Session_ME.gI().clearSendingMessage();
        Session_ME.clearReceivedMessages();
        Session_ME2.gI().close();
        Session_ME2.gI().clearSendingMessage();
        Controller.isConnectOK = Controller.isConnectionFail = Controller.isDisconnected = false;
        LoginScr.finishLoginAttempt();
        LoginScr.timeLogin = 0;
        Char.isLoadingMap = false;
        ServerListScreen.isAutoConect = false;
        ServerListScreen.flagServer = 0;
        ServerListScreen.waitToLogin = ServerListScreen.isWait = false;
        ServerListScreen.countDieConnect = 0;
        ServerListScreen.testConnect = 0;
    }

    public static void Reconnect()
    {
        CloseConnection();
        GameCanvas.endDlg();
        connectionPending = true; // Also bound a reconnect after returning to the selected default server.
        connectionStarted = Environment.TickCount;
        attemptedEndpoint = Label;
        GameCanvas.connect();
        GameCanvas.startWaitDlg();
    }

    public static bool HandleConnectionFailure(string message)
    {
        if ((!Enabled && !connectionPending) || !Controller.isMain
            || GameCanvas.currentScreen is GameScr || GameCanvas.currentScreen is CreateCharScr) return false;
        bool editing = GameCanvas.currentDialog is CustomServerDialog;
        CloseConnection();
        // Let the player finish editing rather than replace the form with an old connection error.
        if (!editing) ShowConnectionError(message);
        return true;
    }

    public static void ShowConnectionError(string message)
    {
        connectionPending = false;
        Char.isLoadingMap = false;
        ServerListScreen.isAutoConect = false;
        ServerListScreen.flagServer = 0;
        if (GameCanvas.currentScreen is SplashScr) GameCanvas.serverScreen.switchToMe();
        GameCanvas.closeKeyBoard();
        string fallback = Session_ME.FallbackAttempted ? " Đã thử thêm 127.0.0.1:" + Port + "." : string.Empty;
        GameCanvas.msgdlg.setInfo(message + " Server: " + (attemptedEndpoint ?? Label) + "." + fallback
            + " Kiểm tra địa chỉ/cổng, cùng Wi-Fi với máy chủ và firewall.",
            new Command("Thử lại", connectionActions, 1, null), null,
            new Command("Về mặc định", connectionActions, 2, null));
        GameCanvas.msgdlg.show();
    }

    public static bool Validate(string inputHost, string inputPort, out string host, out int port, out string error)
    {
        host = (inputHost ?? string.Empty).Trim().ToLowerInvariant();
        port = 0;
        error = "Địa chỉ phải là IPv4 hoặc hostname hợp lệ (chữ, số, dấu chấm, gạch ngang).";
        if (host.Length == 0) { error = "Vui lòng nhập địa chỉ máy chủ."; return false; }
        if (host.Length > 253) return false;
        bool numeric = true;
        foreach (char c in host)
        {
            bool digit = c >= '0' && c <= '9';
            if (!digit && c != '.') numeric = false;
            if (!digit && !(c >= 'a' && c <= 'z') && c != '.' && c != '-') return false;
        }
        string[] labels = host.Split('.');
        if (numeric)
        {
            if (labels.Length != 4) return false;
            foreach (string label in labels)
            {
                byte octet;
                if (label.Length == 0 || (label.Length > 1 && label[0] == '0')
                    || !byte.TryParse(label, NumberStyles.None, CultureInfo.InvariantCulture, out octet)) return false;
            }
        }
        else
        {
            foreach (string label in labels)
                if (label.Length == 0 || label.Length > 63 || label[0] == '-' || label[label.Length - 1] == '-') return false;
        }
        if (!int.TryParse((inputPort ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out port)
            || port < 1 || port > 65535)
        {
            error = "Cổng phải là số từ 1 đến 65535.";
            return false;
        }
        error = null;
        return true;
    }
}
