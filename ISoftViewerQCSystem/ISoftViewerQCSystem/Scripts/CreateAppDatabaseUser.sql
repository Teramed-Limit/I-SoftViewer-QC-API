-- ============================================================
-- ISoftViewerQCSystem 專用資料庫帳號建立腳本
--
-- 目的：建立符合最小權限原則的應用程式專用帳號
-- 風險：H002 - 避免使用 SA 帳號連接資料庫
--
-- 使用方式：
-- 1. 以 SA 或具有足夠權限的帳號登入 SQL Server
-- 2. 修改下方的密碼為強密碼
-- 3. 執行此腳本
-- 4. 更新應用程式的連線字串使用新帳號
-- ============================================================

USE [master]
GO

-- ============================================================
-- 1. 建立 SQL Server Login
-- ============================================================

-- 請修改此密碼為符合安全標準的強密碼
-- 建議：至少 16 個字元，包含大小寫字母、數字和特殊符號
DECLARE @Password NVARCHAR(128) = N'ChangeThisToStrongPassword!2024';
DECLARE @LoginName NVARCHAR(128) = N'isoftviewer_app';

-- 檢查 Login 是否已存在
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
BEGIN
    DECLARE @SQL NVARCHAR(MAX) = N'CREATE LOGIN [' + @LoginName + N'] WITH PASSWORD = N''' + @Password + N''',
        DEFAULT_DATABASE = [TeraLinkaServer],
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = ON';
    EXEC sp_executesql @SQL;
    PRINT 'Login [isoftviewer_app] 已建立';
END
ELSE
BEGIN
    PRINT 'Login [isoftviewer_app] 已存在';
END
GO

-- ============================================================
-- 2. 在應用程式資料庫中建立使用者
-- ============================================================

USE [TeraLinkaServer]
GO

-- 檢查使用者是否已存在
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'isoftviewer_app')
BEGIN
    CREATE USER [isoftviewer_app] FOR LOGIN [isoftviewer_app];
    PRINT '使用者 [isoftviewer_app] 已建立';
END
ELSE
BEGIN
    PRINT '使用者 [isoftviewer_app] 已存在';
END
GO

-- ============================================================
-- 3. 授予基本資料操作權限
-- ============================================================

-- 授予 dbo schema 的基本 CRUD 權限
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO [isoftviewer_app];
PRINT '已授予 dbo schema 的 SELECT, INSERT, UPDATE, DELETE 權限';
GO

-- ============================================================
-- 4. 授予 View 存取權限
-- ============================================================

-- 如果有自訂 Schema 的 View，需要額外授權
-- 以下為常見的 View 權限授予

-- 授予 View 查詢權限（如果 View 在不同 Schema）
-- GRANT SELECT ON [your_schema].[SearchImagePathView] TO [isoftviewer_app];
-- GRANT SELECT ON [your_schema].[SearchPatientStudyView] TO [isoftviewer_app];
-- GRANT SELECT ON [your_schema].[QCOperationRecordView] TO [isoftviewer_app];

GO

-- ============================================================
-- 5. 授予執行預存程序權限（如果有使用）
-- ============================================================

-- 如果應用程式使用預存程序，取消下方註解並修改
-- GRANT EXECUTE ON SCHEMA::dbo TO [isoftviewer_app];

GO

-- ============================================================
-- 6. 明確拒絕危險權限（防禦性設定）
-- ============================================================

-- 拒絕結構變更權限
DENY ALTER ON SCHEMA::dbo TO [isoftviewer_app];
DENY CREATE TABLE TO [isoftviewer_app];
DENY CREATE VIEW TO [isoftviewer_app];
DENY CREATE PROCEDURE TO [isoftviewer_app];
DENY CREATE FUNCTION TO [isoftviewer_app];

-- 拒絕刪除結構權限
DENY DROP ON SCHEMA::dbo TO [isoftviewer_app];

-- 拒絕資料庫級別權限
DENY ALTER ANY USER TO [isoftviewer_app];
DENY ALTER ANY ROLE TO [isoftviewer_app];
DENY ALTER ANY SCHEMA TO [isoftviewer_app];
DENY BACKUP DATABASE TO [isoftviewer_app];
DENY BACKUP LOG TO [isoftviewer_app];

PRINT '已拒絕危險權限';
GO

-- ============================================================
-- 7. 驗證權限設定
-- ============================================================

-- 檢視使用者的權限
PRINT '========== 使用者權限報告 =========='
SELECT
    dp.name AS [使用者],
    dp.type_desc AS [類型],
    p.permission_name AS [權限],
    p.state_desc AS [狀態],
    COALESCE(o.name, s.name, 'DATABASE') AS [物件]
FROM sys.database_principals dp
LEFT JOIN sys.database_permissions p ON dp.principal_id = p.grantee_principal_id
LEFT JOIN sys.objects o ON p.major_id = o.object_id
LEFT JOIN sys.schemas s ON p.major_id = s.schema_id AND p.class = 3
WHERE dp.name = 'isoftviewer_app'
ORDER BY p.state_desc, p.permission_name;
GO

-- ============================================================
-- 8. 測試帳號連線（選擇性）
-- ============================================================

-- 取消註解以下命令來測試新帳號（需要使用 SSMS 或其他工具）
/*
-- 使用新帳號連線測試
-- Connection String: Server=YOUR_SERVER;Database=TeraLinkaServer;User Id=isoftviewer_app;Password=YOUR_PASSWORD;TrustServerCertificate=True;

-- 測試 SELECT 權限
SELECT TOP 1 * FROM DicomPatient;

-- 測試 INSERT 權限（請使用測試資料）
-- INSERT INTO TestTable (Col1) VALUES ('test');

-- 測試是否無法 DROP（應該失敗）
-- DROP TABLE DicomPatient; -- 這應該會失敗
*/

GO

PRINT '============================================================';
PRINT '腳本執行完成！';
PRINT '';
PRINT '下一步驟：';
PRINT '1. 請更新 appsettings.Secrets.json 或 appsettings.Encrypted.json';
PRINT '2. 將 DBUserID 改為 "isoftviewer_app"';
PRINT '3. 將 DBPassword 改為您設定的強密碼';
PRINT '4. 更新 ConnectionString 使用新帳號';
PRINT '5. 重新啟動應用程式並測試功能';
PRINT '============================================================';
GO
