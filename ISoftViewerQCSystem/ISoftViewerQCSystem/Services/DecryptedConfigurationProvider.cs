using System.Security.Cryptography;

namespace ISoftViewerQCSystem.Services;

/// <summary>
/// 解密配置提供者 - 自動解密 DPAPI 加密的配置值
/// 使用遞迴掃描方式，根據 key 名稱判斷是否為敏感欄位
/// </summary>
public class DecryptedConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationRoot _configuration;

    /// <summary>
    /// 敏感 key 名稱清單（不區分大小寫）
    /// 與 AutoEncryptionService 保持一致
    /// </summary>
    private static readonly HashSet<string> SensitiveKeyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConnectionString",
        "SecretKey",
        "DBPassword",
        "Password",
        "Secret",
        "ApiKey",
        "PrivateKey",
        "AccessKey",
        "Token"
    };

    public DecryptedConfigurationProvider(IConfigurationRoot configuration)
    {
        _configuration = configuration;
    }

    public override void Load()
    {
        // 遞迴掃描所有配置，解密敏感欄位
        ScanAndDecrypt(_configuration.GetChildren(), "");
    }

    /// <summary>
    /// 遞迴掃描配置並解密敏感欄位
    /// </summary>
    private void ScanAndDecrypt(IEnumerable<IConfigurationSection> sections, string parentPath)
    {
        foreach (var section in sections)
        {
            var fullPath = string.IsNullOrEmpty(parentPath) ? section.Key : $"{parentPath}:{section.Key}";
            var children = section.GetChildren().ToList();

            if (children.Count > 0)
            {
                // 有子節點，繼續遞迴
                ScanAndDecrypt(children, fullPath);
            }
            else
            {
                // 葉節點，檢查是否需要解密
                var value = section.Value;
                if (value != null && IsSensitiveKey(section.Key) && SecureConfigurationService.IsEncrypted(value))
                {
                    try
                    {
                        Data[fullPath] = SecureConfigurationService.Decrypt(value);
                    }
                    catch (CryptographicException)
                    {
                        // 如果解密失敗，保留原值（讓應用程式在啟動時報錯）
                        Data[fullPath] = value;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判斷 key 名稱是否為敏感欄位
    /// </summary>
    private static bool IsSensitiveKey(string keyName)
    {
        return SensitiveKeyNames.Contains(keyName);
    }
}

/// <summary>
/// 解密配置來源
/// </summary>
public class DecryptedConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationRoot _configuration;

    public DecryptedConfigurationSource(IConfigurationRoot configuration)
    {
        _configuration = configuration;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DecryptedConfigurationProvider(_configuration);
    }
}

/// <summary>
/// 配置建構器擴充方法
/// </summary>
public static class DecryptedConfigurationExtensions
{
    /// <summary>
    /// 添加 DPAPI 解密支援
    /// 自動掃描所有配置，根據 key 名稱判斷是否需要解密
    /// </summary>
    public static IConfigurationBuilder AddDecryptedConfiguration(this IConfigurationBuilder builder)
    {
        // 先建構目前的配置
        var tempConfig = builder.Build();

        // 檢查是否有任何加密值（遞迴掃描）
        if (HasAnyEncryptedValue(tempConfig.GetChildren()))
        {
            builder.Add(new DecryptedConfigurationSource(tempConfig));
        }

        return builder;
    }

    /// <summary>
    /// 遞迴檢查是否有任何加密值
    /// </summary>
    private static bool HasAnyEncryptedValue(IEnumerable<IConfigurationSection> sections)
    {
        foreach (var section in sections)
        {
            var children = section.GetChildren().ToList();

            if (children.Count > 0)
            {
                // 有子節點，繼續遞迴
                if (HasAnyEncryptedValue(children))
                    return true;
            }
            else
            {
                // 葉節點，檢查是否已加密
                if (section.Value != null && SecureConfigurationService.IsEncrypted(section.Value))
                    return true;
            }
        }

        return false;
    }
}
