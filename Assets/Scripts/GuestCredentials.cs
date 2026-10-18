using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Persist before sending: a lost login response must resume the same guest, not create another.
internal static class GuestCredentials
{
    private const string SecretKey = "guestSecretV1";
    private const string PreferredKey = "guestPreferredV1";
    internal const sbyte Command = -124;
    internal static bool IsDev { get; private set; }
    internal static bool HasToken => !string.IsNullOrEmpty(Rms.loadRMSString(SecretKey));
    internal static bool Preferred => Rms.loadRMSString(PreferredKey) == "guest";

    internal static string GetSecret()
    {
        string secret = Rms.loadRMSString(SecretKey);
        if (string.IsNullOrEmpty(secret)) {
            byte[] bytes = new byte[32];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            StringBuilder text = new StringBuilder(64);
            foreach (byte b in bytes) text.Append(b.ToString("x2"));
            secret = text.ToString();
            Rms.saveRMSString(SecretKey, secret);
            if (Rms.loadRMSString(SecretKey) != secret) throw new InvalidOperationException("Không lưu được dữ liệu khách.");
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(secret, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidOperationException("Dữ liệu khách bị lỗi. Không tự tạo khách khác để tránh mất nhân vật.");
        return secret;
    }

    internal static string GetDevice()
    {
        string device = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(device) || device == SystemInfo.unsupportedIdentifier)
            throw new InvalidOperationException("Không đọc được mã thiết bị. Vui lòng thử lại.");
        return device;
    }

    internal static void Accepted(bool dev) {
        IsDev = dev;
        Rms.saveRMSString(PreferredKey, "guest");
    }
    internal static void NormalLogin() {
        IsDev = false;
        Rms.saveRMSString(PreferredKey, "account");
    }

    // Merely replaces a local test credential. Production UNIQUE still rejects a second guest.
    internal static void NewDevGuest()
    {
        if (!IsDev) return;
        Rms.saveRMSString(SecretKey, string.Empty);
        IsDev = false;
    }
}
