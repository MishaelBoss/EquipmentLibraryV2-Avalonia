using System.Security.Cryptography;
using System.Text;

namespace EquipmentLibraryV2_Avalonia.Infrastructure;

internal static class SettingsProtector
{
    private const string KeyFileName = "settings.key";
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private static string KeyPath => Path.Combine(AppPaths.UserDataDir, KeyFileName);

    public static string Encrypt(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return string.Empty;

        try
        {
            var key = GetOrCreateKey();

            var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
            var plain = Encoding.UTF8.GetBytes(plaintext);
            var cipher = new byte[plain.Length];
            var tag = new byte[TagSizeBytes];

            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plain, cipher, tag);

            var result = new byte[nonce.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, result, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length + cipher.Length, tag.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to encrypt settings value; storing as-is");
            return plaintext;
        }
    }

    public static string? TryDecrypt(string? stored)
    {
        if (string.IsNullOrEmpty(stored))
            return null;

        try
        {
            var key = GetOrCreateKey();
            var data = Convert.FromBase64String(stored);

            if (data.Length < NonceSizeBytes + TagSizeBytes)
                return null;

            var nonce = data.AsSpan(0, NonceSizeBytes);
            var tag = data.AsSpan(data.Length - TagSizeBytes, TagSizeBytes);
            var cipher = data.AsSpan(NonceSizeBytes, data.Length - NonceSizeBytes - TagSizeBytes);

            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, cipher, tag, plain);

            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to decrypt stored settings value");
            return null;
        }
    }

    private static byte[] GetOrCreateKey()
    {
        var path = KeyPath;

        if (File.Exists(path))
        {
            var existing = File.ReadAllBytes(path);
            if (existing.Length == KeySizeBytes)
                return existing;
        }

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        File.WriteAllBytes(path, key);

        if (OperatingSystem.IsWindows()) return key;
        
        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to set restrictive permissions on key file {Path}", path);
        }

        return key;
    }
}