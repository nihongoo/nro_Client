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
