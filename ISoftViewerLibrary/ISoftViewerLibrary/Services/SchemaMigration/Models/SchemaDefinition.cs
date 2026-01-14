using System;
using System.Collections.Generic;
using System.Data;

namespace ISoftViewerLibrary.Services.SchemaMigration.Models
{
    /// <summary>
    /// 資料表定義
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// 資料表名稱
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Schema 名稱 (預設 dbo)
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// 欄位定義列表
        /// </summary>
        public List<ColumnDefinition> Columns { get; set; } = new();

        /// <summary>
        /// 索引定義列表
        /// </summary>
        public List<IndexDefinition> Indexes { get; set; } = new();
    }

    /// <summary>
    /// 欄位定義
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// 欄位名稱
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// SQL Server 資料型別 (如: NVARCHAR(50), INT, DATETIME2, etc.)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 是否允許 NULL
        /// </summary>
        public bool IsNullable { get; set; } = true;

        /// <summary>
        /// 是否為主鍵
        /// </summary>
        public bool IsPrimaryKey { get; set; } = false;

        /// <summary>
        /// 是否為 Identity 欄位
        /// </summary>
        public bool IsIdentity { get; set; } = false;

        /// <summary>
        /// Identity 種子值
        /// </summary>
        public int IdentitySeed { get; set; } = 1;

        /// <summary>
        /// Identity 增量值
        /// </summary>
        public int IdentityIncrement { get; set; } = 1;

        /// <summary>
        /// 預設值
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 欄位說明
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 索引定義
    /// </summary>
    public class IndexDefinition
    {
        /// <summary>
        /// 索引名稱
        /// </summary>
        public string IndexName { get; set; }

        /// <summary>
        /// 索引欄位
        /// </summary>
        public List<string> Columns { get; set; } = new();

        /// <summary>
        /// 是否為唯一索引
        /// </summary>
        public bool IsUnique { get; set; } = false;

        /// <summary>
        /// 是否為聚集索引
        /// </summary>
        public bool IsClustered { get; set; } = false;
    }

    /// <summary>
    /// Schema 版本記錄
    /// </summary>
    public class SchemaMigrationHistory
    {
        /// <summary>
        /// 遷移 ID
        /// </summary>
        public int MigrationId { get; set; }

        /// <summary>
        /// 版本號
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 遷移描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 執行的 SQL 腳本
        /// </summary>
        public string SqlScript { get; set; }

        /// <summary>
        /// 執行時間
        /// </summary>
        public DateTime AppliedAt { get; set; }

        /// <summary>
        /// 執行結果
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 欄位變更類型
    /// </summary>
    public enum ColumnChangeType
    {
        Add,
        Modify,
        Drop
    }

    /// <summary>
    /// 欄位變更定義
    /// </summary>
    public class ColumnChange
    {
        /// <summary>
        /// 變更類型
        /// </summary>
        public ColumnChangeType ChangeType { get; set; }

        /// <summary>
        /// 欄位定義
        /// </summary>
        public ColumnDefinition Column { get; set; }

        /// <summary>
        /// 舊欄位名稱 (用於重新命名)
        /// </summary>
        public string OldColumnName { get; set; }
    }
}
