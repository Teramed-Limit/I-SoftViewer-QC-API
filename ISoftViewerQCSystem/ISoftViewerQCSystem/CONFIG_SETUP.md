# 配置設定指南

本專案使用多層配置機制來保護敏感資訊。請依照以下步驟設定您的開發或生產環境。

## 配置優先級（由低到高）

1. `appsettings.json` - 基礎配置（已提交到 Git）
2. `appsettings.{Environment}.json` - 環境特定配置
3. `appsettings.Secrets.json` - 敏感資訊（**不提交到 Git**，僅開發環境使用）
4. `appsettings.Encrypted.json` - DPAPI 加密的敏感資訊（**生產環境自動產生**）
5. 環境變數 - 最高優先級

---

## 自動加密機制（v2.0）

從 v2.0 開始，程式啟動時會自動處理敏感配置的加密，無需手動執行腳本。

### 行為規則

| 環境 | 條件 | 動作 |
|------|------|------|
| Development | - | 不執行加密，直接使用 `appsettings.Secrets.json` |
| 非 Development | 存在 `Secrets.json` + 不存在 `Encrypted.json` | 自動加密 → 刪除 `Secrets.json` |
| 非 Development | 同時存在兩檔案 | 刪除 `Secrets.json`（使用已加密版本） |
| 非 Development | 只有 `Encrypted.json` | 正常讀取加密配置 |
| 非 Development | 加密失敗 | 停止啟動並報錯 |

### 敏感欄位自動偵測

系統會自動加密/解密以下 key 名稱的欄位（不區分大小寫）：

- `ConnectionString`
- `SecretKey`
- `DBPassword`
- `Password`
- `Secret`
- `ApiKey`
- `PrivateKey`
- `AccessKey`
- `Token`

這些 key 可以出現在 JSON 的任何層級，系統會遞迴掃描並處理。

### 部署流程

1. 在目標伺服器上建立 `appsettings.Secrets.json`（含明文敏感資訊）
2. 設定環境變數 `ASPNETCORE_ENVIRONMENT=Production`
3. 啟動程式
4. 程式自動：
   - 加密敏感欄位 → 產生 `appsettings.Encrypted.json`
   - 刪除 `appsettings.Secrets.json`
   - 正常啟動

---

## 快速開始：部署腳本

使用 PowerShell 腳本設定資料庫帳號和檔案權限：

### 基本用法

```powershell
# 在 IIS 伺服器上以管理員身份執行
cd ISoftViewerQCSystem\Scripts
.\Deploy-SecureConfig.ps1 -ServerName "YOUR_SQL_SERVER" -AppPoolName "YourAppPoolName"
```

### 完整範例

```powershell
# 開發環境（自動生成密碼）
.\Deploy-SecureConfig.ps1 `
    -ServerName "localhost\SQLEXPRESS" `
    -DatabaseName "TeraLinkaServer" `
    -AppPoolName "ISoftViewerQCSystem"

# 生產環境（指定密碼）
$appPwd = Read-Host -AsSecureString "輸入應用程式資料庫密碼"
.\Deploy-SecureConfig.ps1 `
    -ServerName "PROD-SQL01" `
    -DatabaseName "TeraLinkaServer" `
    -AppUserPassword $appPwd `
    -AppPoolName "QCSystemPool"

# 跳過資料庫設定（帳號已存在）
.\Deploy-SecureConfig.ps1 `
    -ServerName "PROD-SQL01" `
    -AppPoolName "QCSystemPool" `
    -SkipDatabaseSetup
```

### 腳本執行的操作

| 步驟 | 說明 | 對應安全修復 |
|------|------|------------|
| 1 | 建立專用資料庫帳號 `isoftviewer_app` | H002 |
| 2 | 設定最小權限（只有 CRUD） | H002 |
| 3 | 設定 NTFS 檔案權限 | H001 |

> **注意**：加密步驟已由程式自動處理，無需手動執行。

### CI/CD 整合

```yaml
# Azure DevOps Pipeline 範例
- task: PowerShell@2
  displayName: 'Deploy Secure Config'
  inputs:
    targetType: 'filePath'
    filePath: 'Scripts/Deploy-SecureConfig.ps1'
    arguments: >
      -ServerName "$(SqlServerName)"
      -DatabaseName "$(DatabaseName)"
      -AppPoolName "$(AppPoolName)"
      -SkipDatabaseSetup
```

```yaml
# GitHub Actions 範例
- name: Deploy Secure Config
  shell: pwsh
  run: |
    .\Scripts\Deploy-SecureConfig.ps1 `
      -ServerName "${{ secrets.SQL_SERVER }}" `
      -AppPoolName "${{ vars.APP_POOL_NAME }}" `
      -SkipDatabaseSetup
```

---

## 手動部署方法

如果無法使用自動化腳本，請依照以下步驟手動設定。

---

## 方法一：DPAPI 自動加密（推薦用於 IIS 生產環境）

此方法使用 Windows DPAPI (Data Protection API) 加密敏感配置值。加密的資料綁定到特定機器，即使配置檔案被複製到其他機器也無法解密。

### 優點

- 配置檔案中的敏感資訊已加密，即使被讀取也無法取得明文
- 無需額外的密鑰管理基礎設施
- 與 Windows Server / IIS 完美整合
- **程式啟動時自動加密，無需手動操作**

### 部署步驟

#### 1. 準備明文配置

在 IIS 伺服器上建立 `appsettings.Secrets.json`：

```json
{
  "TeraLinkaAuth": {
    "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "Jwt": {
      "SecretKey": "YOUR_JWT_SECRET_KEY_BASE64_ENCODED"
    }
  },
  "Database": {
    "ServerName": "YOUR_SERVER",
    "DatabaseName": "YOUR_DB",
    "DBUserID": "YOUR_USER",
    "DBPassword": "YOUR_PASSWORD"
  }
}
```

#### 2. 啟動程式

確保環境變數 `ASPNETCORE_ENVIRONMENT` 不是 `Development`，然後啟動程式：

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet ISoftViewerQCSystem.dll
```

程式會自動：
- 讀取 `appsettings.Secrets.json`
- 加密敏感欄位
- 產生 `appsettings.Encrypted.json`
- 刪除 `appsettings.Secrets.json`

#### 3. 設定檔案權限（重要！）

限制 `appsettings.Encrypted.json` 的存取權限，只允許 IIS 應用程式池帳戶讀取：

```powershell
# 移除 Everyone 權限
icacls appsettings.Encrypted.json /remove Everyone

# 只允許應用程式池帳戶讀取（將 YourAppPoolName 替換為實際名稱）
icacls appsettings.Encrypted.json /grant "IIS AppPool\YourAppPoolName:(R)"

# 允許管理員完全控制（用於維護）
icacls appsettings.Encrypted.json /grant Administrators:(F)
```

### 手動加密工具（選用）

如果需要手動加密/解密，可使用內建工具：

| 命令 | 說明 |
|------|------|
| `--encrypt-config` | 互動式加密模式 |
| `--encrypt-value <明文>` | 加密單一值 |
| `--decrypt-value <加密值>` | 解密單一值 |
| `--help-encryption` | 顯示加密工具說明 |

### 注意事項

- **機器綁定**：DPAPI 加密綁定到特定機器。如果需要在不同伺服器部署，必須在每台伺服器上重新執行加密
- **備份**：保留 `appsettings.Secrets.json` 的安全備份（離線存放），以便在新伺服器上重新加密
- **更新配置**：如需更新密碼，重新建立 `appsettings.Secrets.json` 並重啟程式即可

---

## 方法二：使用 appsettings.Secrets.json（推薦用於開發環境）

### 步驟

1. 複製範本檔案：
   ```bash
   cp appsettings.Secrets.json.example appsettings.Secrets.json
   ```

2. 編輯 `appsettings.Secrets.json`，填入實際的敏感資訊：
   ```json
   {
     "TeraLinkaAuth": {
       "ConnectionString": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
       "Jwt": {
         "SecretKey": "YOUR_JWT_SECRET_KEY_BASE64_ENCODED"
       }
     },
     "Database": {
       "ServerName": "YOUR_SERVER",
       "DatabaseName": "YOUR_DB",
       "DBUserID": "YOUR_USER",
       "DBPassword": "YOUR_PASSWORD"
     }
   }
   ```

3. 確認 `.gitignore` 已排除此檔案（已預設排除）

---

## 方法二：使用環境變數（推薦用於生產環境）

ASP.NET Core 會自動將環境變數對應到配置。使用雙底線 `__` 來表示配置層級。

### 必要的環境變數

| 環境變數名稱 | 說明 | 範例 |
|-------------|------|------|
| `TeraLinkaAuth__ConnectionString` | 認證服務資料庫連線字串 | `Server=...;Password=...` |
| `TeraLinkaAuth__Jwt__SecretKey` | JWT 簽章密鑰（Base64） | `YWJjZGVm...` |
| `Database__ServerName` | 資料庫伺服器名稱 | `localhost\SQLEXPRESS` |
| `Database__DatabaseName` | 資料庫名稱 | `TeraLinkaServer` |
| `Database__DBUserID` | 資料庫使用者 | `app_user` |
| `Database__DBPassword` | 資料庫密碼 | `YourStrongPassword` |

### Windows 設定環境變數

```powershell
# PowerShell（當前 Session）
$env:TeraLinkaAuth__ConnectionString = "Server=...;Password=..."
$env:Database__DBPassword = "YourPassword"

# 永久設定（系統環境變數）
[Environment]::SetEnvironmentVariable("Database__DBPassword", "YourPassword", "Machine")
```

### Linux/Docker 設定環境變數

```bash
# Linux
export TeraLinkaAuth__ConnectionString="Server=...;Password=..."
export Database__DBPassword="YourPassword"

# Docker Compose
environment:
  - TeraLinkaAuth__ConnectionString=Server=...;Password=...
  - Database__DBPassword=YourPassword
```

---

## 方法三：使用 .NET User Secrets（開發環境）

User Secrets 會將敏感資訊存在使用者目錄，不會進入專案目錄。

### 初始化（首次使用）

```bash
cd ISoftViewerQCSystem
dotnet user-secrets init
```

### 設定密鑰

```bash
dotnet user-secrets set "Database:DBPassword" "YourPassword"
dotnet user-secrets set "Database:DBUserID" "app_user"
dotnet user-secrets set "TeraLinkaAuth:ConnectionString" "Server=...;Password=..."
dotnet user-secrets set "TeraLinkaAuth:Jwt:SecretKey" "YourBase64SecretKey"
```

### 查看已設定的密鑰

```bash
dotnet user-secrets list
```

---

## 生成安全的 JWT Secret Key

JWT Secret Key 應至少 256 bits（32 bytes）。可使用以下方式生成：

### PowerShell

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))
```

### C#

```csharp
var key = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
Console.WriteLine(key);
```

### Linux/macOS

```bash
openssl rand -base64 64
```

---

## 建立專用資料庫帳號（重要！）

**安全風險 H002：** 使用 SA 帳號連接資料庫違反最小權限原則。SA 帳號擁有完整資料庫權限，若應用程式被入侵，攻擊者將獲得資料庫的完全控制權。

### 為何不能使用 SA 帳號？

| 風險 | 說明 |
|------|------|
| 完全控制權 | SA 可以刪除任何資料庫、修改任何資料 |
| 權限擴張 | 攻擊者可以建立新帳號、提升權限 |
| 無法審計 | 難以區分正常應用程式操作與惡意操作 |
| 違規風險 | 不符合 PCI-DSS、HIPAA 等合規要求 |

### 建立專用帳號步驟

#### 1. 執行 SQL 腳本

使用 SQL Server Management Studio (SSMS) 或其他工具，以 SA 帳號執行：

```
Scripts/CreateAppDatabaseUser.sql
```

**執行前請務必：**
- 修改腳本中的密碼為強密碼（至少 16 個字元）
- 確認資料庫名稱正確

#### 2. 腳本會建立的權限

| 權限類型 | 權限 |
|---------|------|
| 允許 | SELECT, INSERT, UPDATE, DELETE（資料操作） |
| 拒絕 | CREATE, ALTER, DROP（結構變更） |
| 拒絕 | BACKUP, ALTER ANY USER（管理權限） |

#### 3. 更新應用程式配置

修改 `appsettings.Secrets.json` 或 `appsettings.Encrypted.json`：

```json
{
  "Database": {
    "DBUserID": "isoftviewer_app",
    "DBPassword": "YourStrongPassword"
  },
  "TeraLinkaAuth": {
    "ConnectionString": "Server=YOUR_SERVER;Database=TeraLinkaServer;User Id=isoftviewer_app;Password=YourStrongPassword;TrustServerCertificate=True;"
  }
}
```

#### 4. 驗證帳號權限

在 SSMS 中執行以下查詢確認權限正確：

```sql
-- 以 isoftviewer_app 帳號登入後執行
-- 應該成功
SELECT TOP 1 * FROM DicomPatient;

-- 應該失敗
DROP TABLE DicomPatient;  -- 權限不足
```

### 多環境帳號管理

| 環境 | 建議帳號名稱 | 權限層級 |
|------|-------------|---------|
| 開發環境 | isoftviewer_dev | 較寬鬆（可包含 CREATE） |
| 測試環境 | isoftviewer_test | 標準權限 |
| 生產環境 | isoftviewer_app | 最小權限 |

---

## 安全最佳實踐

1. **永遠不要**將 `appsettings.Secrets.json` 提交到版本控制
2. **永遠不要**在程式碼中硬編碼密碼或密鑰
3. **永遠不要**使用 SA 帳號連接生產資料庫（使用專用帳號）
4. 生產環境使用專用的資料庫帳號，只給予必要權限
5. 定期輪換密碼和密鑰
6. 使用強密碼（至少 16 個字元，包含大小寫、數字、特殊符號）
7. 考慮使用 Azure Key Vault 或 HashiCorp Vault 管理生產環境密鑰

---

## 驗證配置

啟動應用程式時，如果敏感配置未設定，將會在日誌中看到警告或錯誤。

確認配置正確載入：

```csharp
// 在 Startup.cs 或任何服務中
var connectionString = Configuration["TeraLinkaAuth:ConnectionString"];
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("TeraLinkaAuth:ConnectionString is not configured");
}
```

---

## 常見問題

### Q: 為什麼 appsettings.json 中的值是空的？

A: 這是設計如此。敏感資訊應透過 `appsettings.Secrets.json` 或環境變數提供。

### Q: 我在 Docker 中如何設定？

A: 使用 Docker 環境變數或 Docker Secrets：

```yaml
# docker-compose.yml
services:
  api:
    environment:
      - Database__DBPassword=${DB_PASSWORD}
    secrets:
      - db_password

secrets:
  db_password:
    file: ./secrets/db_password.txt
```

### Q: 團隊成員如何取得配置值？

A:
- 開發環境：透過安全管道分享 `appsettings.Secrets.json` 的內容
- 生產環境：由 DevOps 團隊管理環境變數或密鑰管理服務
