using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ISoftViewerQCSystem.Services;

/// <summary>
/// 自動加密服務 - 在非開發環境啟動時自動加密敏感配置
/// </summary>
public static class AutoEncryptionService
{
    /// <summary>
    /// 敏感 key 名稱清單（不區分大小寫）
    /// 遇到這些 key 名稱時會自動加密其值
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

    /// <summary>
    /// 確保配置已加密（非 Development 環境）
    /// </summary>
    /// <param name="env">主機環境</param>
    public static void EnsureConfigurationEncrypted(IHostEnvironment env)
    {
        // Development 環境跳過加密
        if (env.IsDevelopment())
        {
            Log.Information("Development 環境，跳過自動加密");
            return;
        }

        var secretsPath = Path.Combine(env.ContentRootPath, "appsettings.Secrets.json");
        var encryptedPath = Path.Combine(env.ContentRootPath, "appsettings.Encrypted.json");

        // 無明文配置檔，無需處理
        if (!File.Exists(secretsPath))
        {
            return;
        }

        // 已存在加密配置，刪除明文檔案（避免重複加密）
        if (File.Exists(encryptedPath))
        {
            Log.Warning("發現 appsettings.Secrets.json 與 appsettings.Encrypted.json 同時存在，" +
                       "將刪除明文檔案並使用加密配置");
            File.Delete(secretsPath);
            Log.Information("已刪除明文配置 appsettings.Secrets.json");
            return;
        }

        // 執行加密
        Log.Information("偵測到 appsettings.Secrets.json，開始自動加密...");
        EncryptSecretsFile(secretsPath, encryptedPath);

        // 刪除明文檔案
        File.Delete(secretsPath);
        Log.Information("已刪除明文配置 appsettings.Secrets.json");
        Log.Information("自動加密完成，配置已儲存至 appsettings.Encrypted.json");
    }

    /// <summary>
    /// 加密 Secrets 檔案並輸出至 Encrypted 檔案
    /// 遞迴處理整個 JSON，遇到敏感 key 就加密，其餘原樣保留
    /// </summary>
    private static void EncryptSecretsFile(string secretsPath, string encryptedPath)
    {
        var secretsJson = File.ReadAllText(secretsPath);
        var jsonNode = JsonNode.Parse(secretsJson);

        if (jsonNode is JsonObject rootObject)
        {
            ProcessJsonObject(rootObject, "");
        }

        // 寫入加密配置檔
        var options = new JsonSerializerOptions { WriteIndented = true };
        var encryptedJson = jsonNode?.ToJsonString(options) ?? "{}";
        File.WriteAllText(encryptedPath, encryptedJson);
    }

    /// <summary>
    /// 遞迴處理 JSON 物件，加密敏感欄位
    /// </summary>
    /// <param name="jsonObject">要處理的 JSON 物件</param>
    /// <param name="currentPath">當前路徑（用於日誌）</param>
    private static void ProcessJsonObject(JsonObject jsonObject, string currentPath)
    {
        var keys = jsonObject.Select(kvp => kvp.Key).ToList();

        foreach (var key in keys)
        {
            var node = jsonObject[key];
            var fullPath = string.IsNullOrEmpty(currentPath) ? key : $"{currentPath}:{key}";

            switch (node)
            {
                case JsonObject childObject:
                    // 遞迴處理子物件
                    ProcessJsonObject(childObject, fullPath);
                    break;

                case JsonArray childArray:
                    // 遞迴處理陣列中的物件
                    ProcessJsonArray(childArray, fullPath);
                    break;

                case JsonValue jsonValue:
                    // 檢查是否為敏感 key，若是則加密
                    if (IsSensitiveKey(key) && jsonValue.TryGetValue<string>(out var stringValue))
                    {
                        if (!string.IsNullOrEmpty(stringValue) &&
                            !SecureConfigurationService.IsEncrypted(stringValue))
                        {
                            var encrypted = SecureConfigurationService.Encrypt(stringValue);
                            jsonObject[key] = encrypted;
                            Log.Debug("已加密 {Path}", fullPath);
                        }
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 遞迴處理 JSON 陣列
    /// </summary>
    private static void ProcessJsonArray(JsonArray jsonArray, string currentPath)
    {
        for (var i = 0; i < jsonArray.Count; i++)
        {
            var node = jsonArray[i];
            var itemPath = $"{currentPath}[{i}]";

            switch (node)
            {
                case JsonObject childObject:
                    ProcessJsonObject(childObject, itemPath);
                    break;
                case JsonArray childArray:
                    ProcessJsonArray(childArray, itemPath);
                    break;
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
