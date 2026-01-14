using System.Collections.Generic;
using ISoftViewerLibrary.Services.SchemaMigration.Models;

namespace ISoftViewerLibrary.Services.SchemaMigration
{
    /// <summary>
    /// Schema 提供者介面 - 定義需要檢查的資料表結構
    /// </summary>
    public interface ISchemaProvider
    {
        /// <summary>
        /// 取得需要同步的資料表定義
        /// </summary>
        IEnumerable<TableDefinition> GetTableDefinitions();

        /// <summary>
        /// 取得自訂遷移腳本
        /// </summary>
        IEnumerable<CustomMigration> GetCustomMigrations();
    }

    /// <summary>
    /// 自訂遷移腳本
    /// </summary>
    public class CustomMigration
    {
        /// <summary>
        /// 版本號 (唯一識別)
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// SQL 腳本
        /// </summary>
        public string SqlScript { get; set; }
    }
}
