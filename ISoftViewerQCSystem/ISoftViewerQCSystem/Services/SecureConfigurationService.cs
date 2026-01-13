using System.Security.Cryptography;
using System.Text;

namespace ISoftViewerQCSystem.Services;

/// <summary>
/// 安全配置服務 - 使用 DPAPI 加密/解密敏感配置
/// DPAPI 加密的資料只能在同一台機器上解密
/// </summary>
public static class SecureConfigurationService
{
    // 加密標記前綴，用於識別已加密的值
    private const string EncryptedPrefix = "ENCRYPTED:";

    /// <summary>
    /// 加密字串（使用 DPAPI - 僅限當前機器）
    /// </summary>
    /// <param name="plainText">明文</param>
    /// <returns>Base64 編碼的加密字串</returns>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = ProtectedData.Protect(
            plainBytes,
            null,
            DataProtectionScope.LocalMachine); // LocalMachine: 同一台機器的任何使用者都能解密

        return EncryptedPrefix + Convert.ToBase64String(encryptedBytes);
    }

    /// <summary>
    /// 解密字串（使用 DPAPI）
    /// </summary>
    /// <param name="encryptedText">加密的字串（含 ENCRYPTED: 前綴）</param>
    /// <returns>明文</returns>
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        // 如果不是加密的值，直接返回
        if (!IsEncrypted(encryptedText))
            return encryptedText;

        var base64Text = encryptedText[EncryptedPrefix.Length..];
        var encryptedBytes = Convert.FromBase64String(base64Text);
        var plainBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.LocalMachine);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// 檢查值是否已加密
    /// </summary>
    public static bool IsEncrypted(string value)
    {
        return !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix);
    }
}
