using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace WpfHarness.Harness.Storage;

/// <summary>
/// 机器绑定的对称加密：用 MAC 地址派生密钥，加密落在配置文件里的 ApiKey。
/// 换机器无法解密属预期行为（与旧版 chatgpt-client 的策略一致）。
/// </summary>
public static class SecretProtector
{
    private static readonly byte[] s_salt = "wpf-harness.v1"u8.ToArray();

    public static string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return string.Empty;
        using var aes = Aes.Create();
        (aes.Key, aes.IV) = DeriveKeyIv();
        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(cipher);
    }

    public static string Decrypt(string? ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return string.Empty;
        try
        {
            using var aes = Aes.Create();
            (aes.Key, aes.IV) = DeriveKeyIv();
            using var decryptor = aes.CreateDecryptor();
            var bytes = Convert.FromBase64String(ciphertext);
            return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(bytes, 0, bytes.Length));
        }
        catch (FormatException)
        {
            return string.Empty;
        }
        catch (CryptographicException)
        {
            return string.Empty;
        }
    }

    private static (byte[] Key, byte[] IV) DeriveKeyIv()
    {
        var machineKey = GetMachineKey();
        
        var okm = System.Security.Cryptography.HKDF.DeriveKey(HashAlgorithmName.SHA256, Encoding.UTF8.GetBytes(machineKey), 48, s_salt);
        return (okm[..32], okm[32..]);
    }

    private static string GetMachineKey()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up)
            {
                var mac = nic.GetPhysicalAddress().ToString();
                if (mac.Length > 0) return mac;
            }
        }
        return Environment.MachineName;
    }
}
