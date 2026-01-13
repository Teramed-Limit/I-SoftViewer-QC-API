<#
.SYNOPSIS
    ISoftViewerQCSystem 資料庫帳號與權限設定腳本

.DESCRIPTION
    此腳本執行以下安全配置：
    1. 建立專用資料庫帳號（H002 修復）
    2. 設定 NTFS 檔案權限

    注意：DPAPI 加密已由程式啟動時自動執行（AutoEncryptionService），
    不再需要手動執行加密步驟。

.PARAMETER ServerName
    SQL Server 名稱（例如：localhost\SQLEXPRESS）

.PARAMETER DatabaseName
    資料庫名稱（預設：TeraLinkaServer）

.PARAMETER AppUserPassword
    應用程式資料庫帳號的密碼（將提示輸入如未提供）

.PARAMETER AppPoolName
    IIS 應用程式池名稱（用於設定檔案權限）

.PARAMETER SqlAdminUser
    SQL Server 管理員帳號（預設：sa）

.PARAMETER SqlAdminPassword
    SQL Server 管理員密碼（將提示輸入如未提供）

.PARAMETER SkipDatabaseSetup
    跳過資料庫帳號建立（如果已存在）

.EXAMPLE
    .\Deploy-SecureConfig.ps1 -ServerName "localhost\SQLEXPRESS" -AppPoolName "ISoftViewerQCSystem"

.EXAMPLE
    .\Deploy-SecureConfig.ps1 -ServerName "PROD-SQL01" -DatabaseName "TeraLinkaServer" -AppPoolName "QCSystemPool" -SkipDatabaseSetup

.NOTES
    作者: Claude Code Security Automation
    版本: 2.0
    需求:
    - PowerShell 5.1+
    - SQL Server 命令列工具 (sqlcmd) 或 SqlServer PowerShell 模組
    - 管理員權限（設定 NTFS 權限）
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ServerName,

    [Parameter(Mandatory = $false)]
    [string]$DatabaseName = "TeraLinkaServer",

    [Parameter(Mandatory = $false)]
    [SecureString]$AppUserPassword,

    [Parameter(Mandatory = $false)]
    [string]$AppPoolName,

    [Parameter(Mandatory = $false)]
    [string]$SqlAdminUser = "sa",

    [Parameter(Mandatory = $false)]
    [SecureString]$SqlAdminPassword,

    [switch]$SkipDatabaseSetup
)

# ============================================================
# 函式定義
# ============================================================

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function ConvertFrom-SecureStringPlain {
    param([SecureString]$SecureString)
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    }
    finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
    }
}

function Test-SqlConnection {
    param(
        [string]$Server,
        [string]$Database,
        [string]$User,
        [string]$Password
    )

    try {
        $connectionString = "Server=$Server;Database=$Database;User Id=$User;Password=$Password;TrustServerCertificate=True;Connection Timeout=5;"
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

function New-StrongPassword {
    param([int]$Length = 20)

    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*"
    $password = -join ((1..$Length) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })

    # 確保包含各類型字元
    $password = $password.Substring(0, $Length - 4)
    $password += "Aa1!"

    return $password
}

# ============================================================
# 主程式
# ============================================================

$ErrorActionPreference = "Stop"
$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectPath = Split-Path -Parent $ScriptPath

Write-Host @"

  _____ ____         __ _  __     ___                        ___   ____
 |_   _/ ___|  ___  / _| |\ \   / (_) _____      _____ _ __|/ _ \ / ___|
   | | \___ \ / _ \| |_| __\ \ / /| |/ _ \ \ /\ / / _ \ '__| | | | |
   | |  ___) | (_) |  _| |_ \ V / | |  __/\ V  V /  __/ |  | |_| | |___
   |_| |____/ \___/|_|  \__| \_/  |_|\___| \_/\_/ \___|_|   \__\_\\____|

  資料庫帳號與權限設定腳本 v2.0
  (DPAPI 加密已由程式啟動時自動執行)

"@ -ForegroundColor Magenta

Write-Host "專案路徑: $ProjectPath"
Write-Host "SQL Server: $ServerName"
Write-Host "資料庫: $DatabaseName"

# ============================================================
# 步驟 1: 收集必要資訊
# ============================================================

Write-Step "步驟 1/3: 收集配置資訊"

# SQL 管理員密碼
if (-not $SkipDatabaseSetup) {
    if (-not $SqlAdminPassword) {
        $SqlAdminPassword = Read-Host "請輸入 SQL Server 管理員 ($SqlAdminUser) 密碼" -AsSecureString
    }
    $sqlAdminPwd = ConvertFrom-SecureStringPlain $SqlAdminPassword
}

# 應用程式帳號密碼
if (-not $AppUserPassword) {
    $generatePassword = Read-Host "是否自動生成應用程式資料庫密碼？ (Y/n)"
    if ($generatePassword -ne 'n') {
        $appPwd = New-StrongPassword -Length 20
        Write-Host "已生成密碼: $appPwd" -ForegroundColor Yellow
        Write-Host "請妥善保存此密碼！" -ForegroundColor Yellow
    }
    else {
        $AppUserPassword = Read-Host "請輸入應用程式資料庫帳號密碼" -AsSecureString
        $appPwd = ConvertFrom-SecureStringPlain $AppUserPassword
    }
}
else {
    $appPwd = ConvertFrom-SecureStringPlain $AppUserPassword
}

Write-Success "配置資訊收集完成"

# ============================================================
# 步驟 2: 建立資料庫帳號
# ============================================================

if (-not $SkipDatabaseSetup) {
    Write-Step "步驟 2/3: 建立專用資料庫帳號 (H002)"

    $sqlScript = @"
USE [master]
GO

-- 檢查並建立 Login
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'isoftviewer_app')
BEGIN
    CREATE LOGIN [isoftviewer_app] WITH PASSWORD = N'$appPwd',
        DEFAULT_DATABASE = [$DatabaseName],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = ON;
    PRINT 'Login [isoftviewer_app] 已建立';
END
ELSE
BEGIN
    -- 更新密碼
    ALTER LOGIN [isoftviewer_app] WITH PASSWORD = N'$appPwd';
    PRINT 'Login [isoftviewer_app] 密碼已更新';
END
GO

USE [$DatabaseName]
GO

-- 檢查並建立使用者
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'isoftviewer_app')
BEGIN
    CREATE USER [isoftviewer_app] FOR LOGIN [isoftviewer_app];
    PRINT '使用者 [isoftviewer_app] 已建立';
END
GO

-- 授予權限
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [isoftviewer_app];

-- 拒絕危險權限
DENY ALTER ON SCHEMA::dbo TO [isoftviewer_app];
DENY CREATE TABLE TO [isoftviewer_app];
DENY CREATE VIEW TO [isoftviewer_app];
DENY CREATE PROCEDURE TO [isoftviewer_app];
DENY DROP ON SCHEMA::dbo TO [isoftviewer_app];
DENY BACKUP DATABASE TO [isoftviewer_app];
DENY BACKUP LOG TO [isoftviewer_app];

PRINT '權限設定完成';
GO
"@

    $tempSqlFile = Join-Path $env:TEMP "deploy_db_user_$(Get-Random).sql"
    $sqlScript | Out-File -FilePath $tempSqlFile -Encoding UTF8

    try {
        # 嘗試使用 sqlcmd
        $sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
        if ($sqlcmdPath) {
            $result = & sqlcmd -S $ServerName -U $SqlAdminUser -P $sqlAdminPwd -i $tempSqlFile -b 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Success "資料庫帳號建立成功"
            }
            else {
                throw "sqlcmd 執行失敗: $result"
            }
        }
        else {
            # 使用 .NET SqlClient
            Write-Host "使用 .NET SqlClient 執行 SQL..."
            $connectionString = "Server=$ServerName;Database=master;User Id=$SqlAdminUser;Password=$sqlAdminPwd;TrustServerCertificate=True;"
            $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
            $connection.Open()

            # 分割 GO 語句執行
            $batches = $sqlScript -split '\r?\nGO\r?\n'
            foreach ($batch in $batches) {
                if ($batch.Trim()) {
                    $command = $connection.CreateCommand()
                    $command.CommandText = $batch
                    $command.ExecuteNonQuery() | Out-Null
                }
            }
            $connection.Close()
            Write-Success "資料庫帳號建立成功"
        }
    }
    catch {
        Write-Error "建立資料庫帳號失敗: $_"
        Write-Host "請手動執行 Scripts/CreateAppDatabaseUser.sql"
    }
    finally {
        Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
    }

    # 驗證連線
    Write-Host "驗證新帳號連線..."
    if (Test-SqlConnection -Server $ServerName -Database $DatabaseName -User "isoftviewer_app" -Password $appPwd) {
        Write-Success "新帳號連線驗證成功"
    }
    else {
        Write-Warning "無法驗證新帳號連線，請手動確認"
    }
}
else {
    Write-Step "步驟 2/3: 跳過資料庫設定"
    Write-Host "已跳過（使用 -SkipDatabaseSetup 參數）"
}

# ============================================================
# 步驟 3: 設定檔案權限
# ============================================================

Write-Step "步驟 3/3: 設定檔案權限"

$secretsPath = Join-Path $ProjectPath "appsettings.Secrets.json"
$encryptedPath = Join-Path $ProjectPath "appsettings.Encrypted.json"

if ($AppPoolName) {
    $configFiles = @($secretsPath, $encryptedPath) | Where-Object { Test-Path $_ }

    foreach ($file in $configFiles) {
        try {
            # 取得目前 ACL
            $acl = Get-Acl $file

            # 移除 Everyone 權限
            $acl.Access | Where-Object { $_.IdentityReference -like "*Everyone*" } | ForEach-Object {
                $acl.RemoveAccessRule($_) | Out-Null
            }

            # 新增 App Pool 讀取權限
            $appPoolIdentity = "IIS AppPool\$AppPoolName"
            $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
                $appPoolIdentity,
                "Read",
                "Allow"
            )
            $acl.AddAccessRule($accessRule)

            # 套用 ACL
            Set-Acl -Path $file -AclObject $acl

            Write-Success "已設定 $file 權限"
        }
        catch {
            Write-Warning "設定檔案權限失敗: $_"
            Write-Host "請手動執行："
            Write-Host "  icacls `"$file`" /grant `"IIS AppPool\$AppPoolName`":(R)"
        }
    }
}
else {
    Write-Warning "未指定 AppPoolName，跳過檔案權限設定"
    Write-Host "請手動設定檔案權限："
    Write-Host '  icacls appsettings.Encrypted.json /grant "IIS AppPool\YourAppPool":(R)'
}

# ============================================================
# 完成
# ============================================================

Write-Host @"

============================================================
                    部署完成！
============================================================

"@ -ForegroundColor Green

Write-Host "已完成的安全配置："
if (-not $SkipDatabaseSetup) {
    Write-Host "  [H002] 已建立專用資料庫帳號 isoftviewer_app" -ForegroundColor Green
}
if ($AppPoolName) {
    Write-Host "  [檔案權限] 已設定 NTFS 權限" -ForegroundColor Green
}

Write-Host "`n重要資訊（請妥善保存）：" -ForegroundColor Yellow
Write-Host "  資料庫帳號: isoftviewer_app"
Write-Host "  資料庫密碼: $appPwd"

Write-Host "`n下一步驟：" -ForegroundColor Cyan
Write-Host "  1. 建立 appsettings.Secrets.json 配置檔（參考 appsettings.Secrets.json.example）"
Write-Host "  2. 啟動應用程式（非 Development 環境會自動加密配置）"
Write-Host "  3. 確認日誌無錯誤訊息"

Write-Host "`n============================================================`n"
