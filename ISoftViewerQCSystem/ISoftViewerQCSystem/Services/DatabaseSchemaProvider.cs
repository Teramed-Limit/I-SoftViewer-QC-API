using System.Collections.Generic;
using ISoftViewerLibrary.Services.SchemaMigration;
using ISoftViewerLibrary.Services.SchemaMigration.Models;

namespace ISoftViewerQCSystem.Services
{
    /// <summary>
    /// 資料庫 Schema 提供者 - 定義系統所需的資料表結構
    /// 在這裡定義需要自動檢查和建立的資料表和欄位
    /// </summary>
    public class DatabaseSchemaProvider : ISchemaProvider
    {
        /// <summary>
        /// 取得需要同步的資料表定義
        /// </summary>
        public IEnumerable<TableDefinition> GetTableDefinitions()
        {
            // 在這裡定義您需要檢查的資料表
            // 範例：

            // yield return new TableDefinition
            // {
            //     TableName = "SvrConfiguration",
            //     SchemaName = "dbo",
            //     Columns = new List<ColumnDefinition>
            //     {
            //         new() { ColumnName = "ConfigId", DataType = "INT", IsNullable = false, IsPrimaryKey = true, IsIdentity = true },
            //         new() { ColumnName = "ConfigKey", DataType = "NVARCHAR(100)", IsNullable = false },
            //         new() { ColumnName = "ConfigValue", DataType = "NVARCHAR(MAX)", IsNullable = true },
            //         new() { ColumnName = "Description", DataType = "NVARCHAR(500)", IsNullable = true },
            //         new() { ColumnName = "CreateTime", DataType = "DATETIME2", IsNullable = false, DefaultValue = "GETDATE()" },
            //         new() { ColumnName = "UpdateTime", DataType = "DATETIME2", IsNullable = true }
            //     }
            // };

            // DicomOperationNodes 表 - 確保 CFindReqField 欄位存在
            yield return new TableDefinition
            {
                TableName = "DicomOperationNodes",
                SchemaName = "dbo",
                Columns = new List<ColumnDefinition>
                {
                    // 只需要定義要新增的欄位，系統會自動檢查並新增缺少的欄位
                    new() { ColumnName = "CFindReqField", DataType = "NVARCHAR(MAX)", IsNullable = true },
                    new() { ColumnName = "IsLocalStoreService", DataType = "INT", IsNullable = true, DefaultValue = "0" }
                }
            };
        }

        /// <summary>
        /// 取得自訂遷移腳本
        /// 用於執行特定版本的資料庫變更
        /// </summary>
        public IEnumerable<CustomMigration> GetCustomMigrations()
        {
            // 範例：新增索引
            // yield return new CustomMigration
            // {
            //     Version = "1.0.1",
            //     Description = "為 UserOperationLog 表新增索引",
            //     SqlScript = @"
            //         IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserOperationLog_UserId')
            //         BEGIN
            //             CREATE NONCLUSTERED INDEX [IX_UserOperationLog_UserId]
            //             ON [dbo].[UserOperationLog] ([UserId])
            //         END"
            // };

            // 範例：新增欄位
            // yield return new CustomMigration
            // {
            //     Version = "1.0.2",
            //     Description = "為 SvrConfiguration 表新增 IsEncrypted 欄位",
            //     SqlScript = @"
            //         IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            //                        WHERE TABLE_NAME = 'SvrConfiguration' AND COLUMN_NAME = 'IsEncrypted')
            //         BEGIN
            //             ALTER TABLE [dbo].[SvrConfiguration] ADD [IsEncrypted] BIT NOT NULL DEFAULT 0
            //         END"
            // };

            yield break; // 預設沒有自訂遷移，請依需求新增
        }
    }
}
