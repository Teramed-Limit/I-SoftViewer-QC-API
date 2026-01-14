# ASP.NET Core API 安全性稽核報告

## 專案資訊

| 項目      | 內容                |
| --------- | ------------------- |
| 專案名稱  | ISoftViewerQCSystem |
| .NET 版本 | .NET 8.0            |
| 檢查日期  | 2026-01-12          |
| 檢查範圍  | 完整專案安全性稽核  |

---

## 風險等級統計

| 等級                      | 數量  | 說明         |
| ------------------------- | ----- | ------------ |
| :red_circle: 高風險       | 4 項  | 需立即修復   |
| :orange_circle: 中風險    | 6 項  | 建議盡快修復 |
| :yellow_circle: 低風險    | 3 項  | 建議改善     |
| :white_check_mark: 已通過 | 12 項 | 符合安全標準 |

---

## 高風險問題

### H001: appsettings.json 包含明文敏感資訊

| 項目     | 內容                                     |
| -------- | ---------------------------------------- |
| 類別     | A02:2021 Cryptographic Failures          |
| 位置     | `appsettings.json:69, 104-107`           |
| 問題描述 | 資料庫連線字串和密碼以明文儲存在配置檔中 |
| 影響     | 若配置檔洩露，攻擊者可直接存取資料庫     |

**問題程式碼:**

```json
"ConnectionString": "Server=...;User Id=sa;Password=admin;...",
"Database": {
    "DBUserID": "sa",
    "DBPassword": "admin"
}
```

**修復建議:**

1. 使用環境變數或 User Secrets 儲存敏感資訊
2. 生產環境使用 Azure Key Vault 或類似服務
3. 移除 appsettings.json 中的明文密碼

```csharp
// 使用環境變數
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

// 或使用 User Secrets (開發環境)
// dotnet user-secrets set "Database:DBPassword" "your-password"
```

---

### H002: 使用 SA 帳號連接資料庫

| 項目     | 內容                                              |
| -------- | ------------------------------------------------- |
| 類別     | A01:2021 Broken Access Control                    |
| 位置     | `appsettings.json:104-107`                        |
| 問題描述 | 使用 SQL Server 的 SA (System Administrator) 帳號 |
| 影響     | SA 帳號擁有完整資料庫權限，違反最小權限原則       |

**修復建議:**

1. 建立專用的應用程式資料庫帳號
2. 只授予必要的權限 (SELECT, INSERT, UPDATE, DELETE)
3. 禁止 DROP, ALTER, CREATE 等管理權限

```sql
-- 建立專用帳號
CREATE LOGIN app_user WITH PASSWORD = 'StrongPassword123!';
CREATE USER app_user FOR LOGIN app_user;

-- 只給必要權限
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO app_user;
```

---

### H003: 缺少 Security Headers

| 項目     | 內容                                         |
| -------- | -------------------------------------------- |
| 類別     | A05:2021 Security Misconfiguration           |
| 位置     | `Startup.cs`                                 |
| 問題描述 | 未設置必要的 HTTP Security Headers           |
| 影響     | 易受 XSS、Clickjacking、MIME-sniffing 等攻擊 |

**修復建議:**
在 `Startup.cs` 的 `Configure` 方法中添加安全標頭中介軟體:

```csharp
// 在 app.UseRouting() 之前添加
app.Use(async (context, next) =>
{
    // 防止 MIME 類型嗅探
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // 防止點擊劫持
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // XSS 保護
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

    // 控制 Referrer 資訊
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; frame-ancestors 'none'");

    // 移除伺服器資訊
    context.Response.Headers.Remove("Server");
    context.Response.Headers.Remove("X-Powered-By");

    await next();
});
```

---

### H004: Path Traversal 潛在風險

| 項目     | 內容                                         |
| -------- | -------------------------------------------- |
| 類別     | A03:2021 Injection                           |
| 位置     | `PacsLogController.cs:74`                    |
| 問題描述 | 日誌路徑由用戶輸入組合，可能導致路徑遍歷攻擊 |
| 影響     | 攻擊者可能讀取系統中的任意檔案               |

**問題程式碼:**

```csharp
string logPath = string.Format(config.LogRootPath + @"\{0}\{1}\{2}.txt",
    logType, studyDate, aeTitle);
```

**修復建議:**

```csharp
[HttpGet("pacslog")]
public ActionResult<string> GetPacsLog(string logType, string aeTitle, string studyDate)
{
    // 驗證輸入只包含允許的字元
    var allowedLogTypes = new[] {
        "DicomCStoreServiceProvider",
        "DicomServiceWorklistProvider",
        "DicocmServiceQRProvider",
        "ServiceJobsManager"
    };

    if (!allowedLogTypes.Contains(logType))
        return BadRequest("Invalid log type");

    // 驗證日期格式
    if (!Regex.IsMatch(studyDate, @"^\d{8}$"))
        return BadRequest("Invalid date format");

    // 驗證 AE Title 只包含允許字元
    if (!Regex.IsMatch(aeTitle, @"^[a-zA-Z0-9_\-]+$"))
        return BadRequest("Invalid AE Title");

    // 使用 Path.Combine 並驗證最終路徑在允許範圍內
    var logPath = Path.Combine(config.LogRootPath, logType, studyDate, $"{aeTitle}.txt");
    var fullPath = Path.GetFullPath(logPath);
    var allowedBasePath = Path.GetFullPath(config.LogRootPath);

    if (!fullPath.StartsWith(allowedBasePath))
        return BadRequest("Invalid path");

    // ... 繼續處理
}
```

---

## 中風險問題

### M001: 缺少 Input Validation 屬性

| 項目     | 內容                                                       |
| -------- | ---------------------------------------------------------- |
| 類別     | A04:2021 Insecure Design                                   |
| 位置     | 所有 DTO 類別                                              |
| 問題描述 | DTO 缺少 `[Required]`, `[MaxLength]`, `[Range]` 等驗證屬性 |

**修復建議:**

```csharp
public class AuthLoginRequest
{
    [Required(ErrorMessage = "使用者名稱為必填")]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "使用者名稱只能包含字母、數字和底線")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密碼為必填")]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
```

---

### M002: Swagger UI 在生產環境可能暴露

| 項目     | 內容                                                           |
| -------- | -------------------------------------------------------------- |
| 類別     | A05:2021 Security Misconfiguration                             |
| 位置     | `Startup.cs:241-243`                                           |
| 問題描述 | Swagger UI 只在 Development 環境顯示，但需確認生產環境設定正確 |

**修復建議:**
確保生產環境 `ASPNETCORE_ENVIRONMENT` 設為 `Production`，或添加額外保護:

```csharp
if (env.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "I-SoftViewer-QCSystem v1");
    });
}
// 生產環境若需要 Swagger，應加入認證保護
```

---

### M003: 缺少 Rate Limiting

| 項目     | 內容                                            |
| -------- | ----------------------------------------------- |
| 類別     | A04:2021 Insecure Design                        |
| 位置     | 全域                                            |
| 問題描述 | API 未實作請求頻率限制，易受暴力破解和 DoS 攻擊 |

**修復建議:**

```csharp
// Program.cs 或 Startup.cs
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    // 登入端點更嚴格的限制
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// 使用
app.UseRateLimiter();

// Controller
[EnableRateLimiting("login")]
[HttpPost("login")]
public async Task<IActionResult> Login(...)
```

---

### M004: Access Token 過期時間過短

| 項目     | 內容                                               |
| -------- | -------------------------------------------------- |
| 類別     | A07:2021 Authentication Failures                   |
| 位置     | `appsettings.json:74`                              |
| 問題描述 | AccessTokenExpirationMinutes 設為 1 分鐘，過於短暫 |

**修復建議:**
建議 Access Token 過期時間設為 15-60 分鐘:

```json
"Jwt": {
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
}
```

---

### M005: 錯誤訊息可能洩露敏感資訊

| 項目     | 內容                                                      |
| -------- | --------------------------------------------------------- |
| 類別     | A05:2021 Security Misconfiguration                        |
| 位置     | 多個 Controller                                           |
| 問題描述 | 部分 catch 區塊直接返回 `e.Message`，可能暴露系統內部資訊 |

**問題程式碼:**

```csharp
catch (Exception e)
{
    return BadRequest(e.Message);  // 可能暴露敏感資訊
}
```

**修復建議:**

```csharp
catch (Exception e)
{
    _logger.LogError(e, "Operation failed");
    return BadRequest(new { error = "操作失敗，請稍後再試" });
}
```

---

### M006: HTTPS 重導向順序問題

| 項目     | 內容                                       |
| -------- | ------------------------------------------ |
| 類別     | A02:2021 Cryptographic Failures            |
| 位置     | `Startup.cs:256`                           |
| 問題描述 | `UseHttpsRedirection` 應該在較早的位置執行 |

**修復建議:**
調整中介軟體順序:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();  // 只在生產環境
    }

    app.UseHttpsRedirection();  // 移到更早的位置

    app.UseDefaultFiles();
    app.UseSpaStaticFiles();
    app.UseStaticFiles();

    app.UseRouting();
    // ...
}
```

---

## 低風險問題

### L001: CORS 配置可能過於寬鬆

| 項目     | 內容                                                             |
| -------- | ---------------------------------------------------------------- |
| 類別     | A01:2021 Broken Access Control                                   |
| 位置     | `Startup.cs:221-228`                                             |
| 問題描述 | CORS 策略名稱為 "AllowAll"，雖然實際使用白名單，但應更名避免誤解 |

**修復建議:**

```csharp
services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", policy =>  // 更明確的名稱
    {
        policy.WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE")  // 明確指定方法
            .WithHeaders("Authorization", "Content-Type")  // 明確指定標頭
            .AllowCredentials();
    });
});
```

---

### L002: 日誌層級配置

| 項目     | 內容                                                            |
| -------- | --------------------------------------------------------------- |
| 類別     | A09:2021 Logging Failures                                       |
| 位置     | `appsettings.json:29`                                           |
| 問題描述 | Serilog 預設層級為 Debug，生產環境應使用 Information 或 Warning |

**修復建議:**
生產環境配置 (`appsettings.Production.json`):

```json
"Serilog": {
    "MinimumLevel": {
        "Default": "Information",
        "Override": {
            "Microsoft.AspNetCore": "Warning"
        }
    }
}
```

---

### L003: AllowedHosts 設為 "\*"

| 項目     | 內容                                        |
| -------- | ------------------------------------------- |
| 類別     | A05:2021 Security Misconfiguration          |
| 位置     | `appsettings.json:67`                       |
| 問題描述 | AllowedHosts 設為 "\*" 允許任何 Host Header |

**修復建議:**
生產環境應明確指定允許的 Host:

```json
"AllowedHosts": "your-domain.com;api.your-domain.com"
```

---

## 已通過項目

| 項目                                  | 說明                                           |
| ------------------------------------- | ---------------------------------------------- |
| :white_check_mark: SQL Injection 防護 | 使用參數化查詢 (AddParameters)                 |
| :white_check_mark: JWT 認證配置       | ValidateIssuer/Audience/Lifetime 皆為 true     |
| :white_check_mark: 密碼雜湊           | 使用 Argon2 演算法                             |
| :white_check_mark: 帳戶鎖定機制       | MaxFailedAttempts: 5, LockoutDuration: 15 分鐘 |
| :white_check_mark: Refresh Token 機制 | 已實作 Token Rotation                          |
| :white_check_mark: API 授權           | 所有 Controller 都有 [Authorize] 屬性          |
| :white_check_mark: Role-based 授權    | 使用 [RequireFunction] 屬性                    |
| :white_check_mark: NuGet 套件         | 無已知漏洞                                     |
| :white_check_mark: 審計日誌           | 已啟用登入/操作記錄                            |
| :white_check_mark: CSRF 保護          | API 使用 JWT Bearer Token                      |
| :white_check_mark: 反序列化安全       | 使用強型別反序列化                             |
| :white_check_mark: Command Injection  | 未發現執行外部命令                             |

---

## 修復優先順序

| 優先級 | 編號 | 問題                       | 預估工時 |
| ------ | ---- | -------------------------- | -------- |
| 1      | H001 | 移除明文密碼，使用環境變數 | 1-2 小時 |
| 2      | H002 | 建立專用資料庫帳號         | 1 小時   |
| 3      | H003 | 添加 Security Headers      | 30 分鐘  |
| 4      | H004 | 修復 Path Traversal 風險   | 1 小時   |
| 5      | M001 | 添加 Input Validation      | 2-4 小時 |
| 6      | M003 | 實作 Rate Limiting         | 1-2 小時 |
| 7      | M005 | 改善錯誤處理               | 1-2 小時 |
| 8      | M006 | 調整中介軟體順序           | 15 分鐘  |
| 9      | M002 | 確認 Swagger 生產環境配置  | 15 分鐘  |
| 10     | M004 | 調整 Token 過期時間        | 5 分鐘   |

---

## 立即執行的安全加固腳本

### 1. 檢查 NuGet 套件漏洞

```bash
dotnet list package --vulnerable
dotnet list package --outdated
```

### 2. 建議安裝的安全套件

```bash
# 添加 Rate Limiting (如果還沒有)
dotnet add package Microsoft.AspNetCore.RateLimiting

# 添加 Security Headers
dotnet add package NWebsec.AspNetCore.Middleware
```

---

## 生產環境部署檢查清單

- [ ] 移除 appsettings.json 中的明文密碼
- [ ] 設置 `ASPNETCORE_ENVIRONMENT=Production`
- [ ] 確認 HTTPS 憑證有效
- [ ] 確認資料庫使用專用帳號
- [ ] 確認防火牆規則正確
- [ ] 確認日誌層級適當
- [ ] 確認備份策略就緒
- [ ] 確認監控告警已設置

---

## 附錄：安全配置範本

### 建議的 Startup.cs 安全配置

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // 1. 全域異常處理
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // 2. Security Headers (最早)
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");
        context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=()");
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        await next();
    });

    // 3. HTTPS 重導向
    app.UseHttpsRedirection();

    // 4. 靜態檔案
    app.UseDefaultFiles();
    app.UseSpaStaticFiles();
    app.UseStaticFiles();

    // 5. 路由
    app.UseRouting();

    // 6. CORS
    app.UseCors("ProductionCors");

    // 7. Rate Limiting
    app.UseRateLimiter();

    // 8. 認證授權
    app.UseAuthentication();
    app.UseAuthorization();

    // 9. 日誌 (只在開發環境詳細記錄)
    if (env.IsDevelopment())
    {
        app.UseSerilogRequestLogging();
    }

    // 10. 端點
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

---

_報告產生日期: 2026-01-12_
_檢查工具: Claude Code Security Audit Skill_
