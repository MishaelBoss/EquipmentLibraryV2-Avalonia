using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EquipmentLibraryV2_Avalonia.Helpers;

public static class CookieHelper
{
    private static readonly byte[] CookieKey = [0x45, 0x4C, 0x43, 0x4F, 0x4F, 0x4B, 0x56, 0x32, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0xAB, 0xCD, 0xEF, 0x01, 0x23, 0x45, 0x67, 0x89];

    public static void WriteEncrypted<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data);
        var plain = Encoding.UTF8.GetBytes(json);

        using var aes = Aes.Create();
        aes.Key = CookieKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var result = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, result);
    }

    public static T? ReadEncrypted<T>(string path) where T : class
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var full = File.ReadAllBytes(path);

            using var aes = Aes.Create();
            aes.Key = CookieKey;
            aes.IV = full.AsSpan(0, 16).ToArray();

            using var decryptor = aes.CreateDecryptor();
            var input = full.AsSpan(16).ToArray();
            var plain = decryptor.TransformFinalBlock(input, 0, input.Length);

            var json = Encoding.UTF8.GetString(plain);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            Delete(path);
            return null;
        }
    }

    public static void Delete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
