using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using ISoftViewerLibrary.Services.SchemaMigration.Models;
using Serilog;

namespace ISoftViewerLibrary.Services.SchemaMigration
{
    /// <summary>
    /// Schema 遷移服務 - 提供資料表和欄位的自動檢查與遷移功能
    /// </summary>
    public class SchemaMigrationService : IDisposable
    {
        private readonly string _connectionString;
        private SqlConnection _connection;
        private const string MigrationHistoryTable = "__SchemaMigrationHistory";

        /// <summary>
        /// 建構函式
        /// </summary>
        public SchemaMigrationService(string serverName, string databaseName, string userId, string password)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverName,
                InitialCatalog = databaseName,
                UserID = userId,
                Password = password
            };
            _connectionString = builder.ConnectionString;
        }

        /// <summary>
        /// 初始化遷移系統 (建立遷移歷史表)
        /// </summary>
        public void Initialize()
        {
            EnsureMigrationHistoryTableExists();
        }

        #region 資料表操作

        /// <summary>
        /// 檢查資料表是否存在
        /// </summary>
        public bool TableExists(string tableName, string schemaName = "dbo")
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            return (int)cmd.ExecuteScalar() > 0;
        }

        /// <summary>
        /// 建立資料表
        /// </summary>
        public bool CreateTable(TableDefinition table)
        {
            if (TableExists(table.TableName, table.SchemaName))
            {
                Log.Information("資料表 {SchemaName}.{TableName} 已存在，跳過建立", table.SchemaName, table.TableName);
                return true;
            }

            var sql = GenerateCreateTableSql(table);
            return ExecuteMigration($"Create table {table.SchemaName}.{table.TableName}", sql);
        }

        /// <summary>
        /// 刪除資料表
        /// </summary>
        public bool DropTable(string tableName, string schemaName = "dbo")
        {
            if (!TableExists(tableName, schemaName))
            {
                Log.Information("資料表 {SchemaName}.{TableName} 不存在，跳過刪除", schemaName, tableName);
                return true;
            }

            var sql = $"DROP TABLE [{schemaName}].[{tableName}]";
            return ExecuteMigration($"Drop table {schemaName}.{tableName}", sql);
        }

        /// <summary>
        /// 確保資料表存在 (不存在則建立)
        /// </summary>
        public bool EnsureTableExists(TableDefinition table)
        {
            return TableExists(table.TableName, table.SchemaName) || CreateTable(table);
        }

        #endregion

        #region 欄位操作

        /// <summary>
        /// 檢查欄位是否存在
        /// </summary>
        public bool ColumnExists(string tableName, string columnName, string schemaName = "dbo")
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = @SchemaName
                  AND TABLE_NAME = @TableName
                  AND COLUMN_NAME = @ColumnName";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@ColumnName", columnName);
            return (int)cmd.ExecuteScalar() > 0;
        }

        /// <summary>
        /// 新增欄位
        /// </summary>
        public bool AddColumn(string tableName, ColumnDefinition column, string schemaName = "dbo")
        {
            if (ColumnExists(tableName, column.ColumnName, schemaName))
            {
                Log.Information("欄位 {TableName}.{ColumnName} 已存在，跳過新增", tableName, column.ColumnName);
                return true;
            }

            var sql = GenerateAddColumnSql(tableName, column, schemaName);
            return ExecuteMigration($"Add column {tableName}.{column.ColumnName}", sql);
        }

        /// <summary>
        /// 修改欄位
        /// </summary>
        public bool ModifyColumn(string tableName, ColumnDefinition column, string schemaName = "dbo")
        {
            if (!ColumnExists(tableName, column.ColumnName, schemaName))
            {
                Log.Warning("欄位 {TableName}.{ColumnName} 不存在，無法修改", tableName, column.ColumnName);
                return false;
            }

            var sql = GenerateModifyColumnSql(tableName, column, schemaName);
            return ExecuteMigration($"Modify column {tableName}.{column.ColumnName}", sql);
        }

        /// <summary>
        /// 刪除欄位
        /// </summary>
        public bool DropColumn(string tableName, string columnName, string schemaName = "dbo")
        {
            if (!ColumnExists(tableName, columnName, schemaName))
            {
                Log.Information("欄位 {TableName}.{ColumnName} 不存在，跳過刪除", tableName, columnName);
                return true;
            }

            // 先移除該欄位的預設值約束
            var dropDefaultSql = GenerateDropDefaultConstraintSql(tableName, columnName, schemaName);
            if (!string.IsNullOrEmpty(dropDefaultSql))
            {
                ExecuteMigration($"Drop default constraint for {tableName}.{columnName}", dropDefaultSql);
            }

            var sql = $"ALTER TABLE [{schemaName}].[{tableName}] DROP COLUMN [{columnName}]";
            return ExecuteMigration($"Drop column {tableName}.{columnName}", sql);
        }

        /// <summary>
        /// 重新命名欄位
        /// </summary>
        public bool RenameColumn(string tableName, string oldColumnName, string newColumnName, string schemaName = "dbo")
        {
            if (!ColumnExists(tableName, oldColumnName, schemaName))
            {
                Log.Warning("欄位 {TableName}.{OldColumnName} 不存在，無法重新命名", tableName, oldColumnName);
                return false;
            }

            var sql = $"EXEC sp_rename '{schemaName}.{tableName}.{oldColumnName}', '{newColumnName}', 'COLUMN'";
            return ExecuteMigration($"Rename column {tableName}.{oldColumnName} to {newColumnName}", sql);
        }

        /// <summary>
        /// 確保欄位存在 (不存在則新增)
        /// </summary>
        public bool EnsureColumnExists(string tableName, ColumnDefinition column, string schemaName = "dbo")
        {
            return ColumnExists(tableName, column.ColumnName, schemaName) || AddColumn(tableName, column, schemaName);
        }

        #endregion

        #region 批次操作

        /// <summary>
        /// 同步資料表 Schema (檢查並新增缺少的欄位)
        /// </summary>
        public bool SyncTableSchema(TableDefinition expectedTable)
        {
            var allSuccess = true;

            // 確保資料表存在
            if (!TableExists(expectedTable.TableName, expectedTable.SchemaName))
            {
                return CreateTable(expectedTable);
            }

            // 檢查每個欄位
            foreach (var column in expectedTable.Columns)
            {
                if (!EnsureColumnExists(expectedTable.TableName, column, expectedTable.SchemaName))
                {
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// 同步多個資料表
        /// </summary>
        public bool SyncAllTables(IEnumerable<TableDefinition> tables)
        {
            var allSuccess = true;
            foreach (var table in tables)
            {
                Log.Information("同步資料表: {SchemaName}.{TableName}", table.SchemaName, table.TableName);
                if (!SyncTableSchema(table))
                {
                    allSuccess = false;
                    Log.Error("同步資料表 {TableName} 失敗", table.TableName);
                }
            }
            return allSuccess;
        }

        /// <summary>
        /// 執行自訂 SQL 遷移
        /// </summary>
        public bool ExecuteCustomMigration(string version, string description, string sql)
        {
            // 檢查是否已執行過
            if (IsMigrationApplied(version))
            {
                Log.Information("遷移 {Version} 已執行過，跳過", version);
                return true;
            }

            return ExecuteMigration(description, sql, version);
        }

        #endregion

        #region 取得現有 Schema 資訊

        /// <summary>
        /// 取得資料表的所有欄位
        /// </summary>
        public List<ColumnDefinition> GetTableColumns(string tableName, string schemaName = "dbo")
        {
            const string sql = @"
                SELECT
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as IsIdentity,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                    WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
                    AND c.TABLE_NAME = pk.TABLE_NAME
                    AND c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @SchemaName AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION";

            var columns = new List<ColumnDefinition>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);
            cmd.Parameters.AddWithValue("@TableName", tableName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var dataType = reader["DATA_TYPE"].ToString();
                var maxLength = reader["CHARACTER_MAXIMUM_LENGTH"] as int?;
                var precision = reader["NUMERIC_PRECISION"] as byte?;
                var scale = reader["NUMERIC_SCALE"] as int?;

                // 組合完整的資料型別
                var fullDataType = dataType.ToUpper() switch
                {
                    "NVARCHAR" or "VARCHAR" or "CHAR" or "NCHAR" =>
                        maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength})",
                    "DECIMAL" or "NUMERIC" => $"{dataType}({precision},{scale})",
                    _ => dataType
                };

                columns.Add(new ColumnDefinition
                {
                    ColumnName = reader["COLUMN_NAME"].ToString(),
                    DataType = fullDataType,
                    IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                    DefaultValue = reader["COLUMN_DEFAULT"]?.ToString(),
                    IsIdentity = Convert.ToInt32(reader["IsIdentity"]) == 1,
                    IsPrimaryKey = Convert.ToInt32(reader["IsPrimaryKey"]) == 1
                });
            }

            return columns;
        }

        /// <summary>
        /// 取得所有資料表名稱
        /// </summary>
        public List<string> GetAllTableNames(string schemaName = "dbo")
        {
            const string sql = @"
                SELECT TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = @SchemaName AND TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            var tables = new List<string>();
            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tables.Add(reader["TABLE_NAME"].ToString());
            }

            return tables;
        }

        #endregion

        #region 私有方法

        private SqlConnection GetConnection()
        {
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new SqlConnection(_connectionString);
                _connection.Open();
            }
            return _connection;
        }

        private void EnsureMigrationHistoryTableExists()
        {
            const string sql = @"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName)
                BEGIN
                    CREATE TABLE [dbo].[__SchemaMigrationHistory] (
                        [MigrationId] INT IDENTITY(1,1) PRIMARY KEY,
                        [Version] NVARCHAR(50) NULL,
                        [Description] NVARCHAR(500) NOT NULL,
                        [SqlScript] NVARCHAR(MAX) NULL,
                        [AppliedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
                        [Success] BIT NOT NULL DEFAULT 1,
                        [ErrorMessage] NVARCHAR(MAX) NULL
                    )
                END";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TableName", MigrationHistoryTable);
            cmd.ExecuteNonQuery();

            Log.Information("Schema 遷移歷史表已確認存在");
        }

        private bool IsMigrationApplied(string version)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM [dbo].[__SchemaMigrationHistory]
                WHERE [Version] = @Version AND [Success] = 1";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Version", version);
            return (int)cmd.ExecuteScalar() > 0;
        }

        private bool ExecuteMigration(string description, string sql, string version = null)
        {
            Log.Information("執行遷移: {Description}", description);
            Log.Debug("SQL: {Sql}", sql);

            using var conn = GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.CommandTimeout = 300; // 5 分鐘超時
                cmd.ExecuteNonQuery();

                // 記錄遷移歷史
                RecordMigrationHistory(conn, transaction, version, description, sql, true, null);

                transaction.Commit();
                Log.Information("遷移成功: {Description}", description);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "遷移失敗: {Description}", description);

                try
                {
                    transaction.Rollback();
                }
                catch
                {
                    // 忽略回滾錯誤
                }

                // 記錄失敗的遷移 (使用新連線)
                try
                {
                    using var newConn = new SqlConnection(_connectionString);
                    newConn.Open();
                    RecordMigrationHistory(newConn, null, version, description, sql, false, ex.Message);
                }
                catch
                {
                    // 忽略記錄錯誤
                }

                return false;
            }
        }

        private void RecordMigrationHistory(SqlConnection conn, SqlTransaction transaction,
            string version, string description, string sql, bool success, string errorMessage)
        {
            const string insertSql = @"
                INSERT INTO [dbo].[__SchemaMigrationHistory]
                ([Version], [Description], [SqlScript], [Success], [ErrorMessage])
                VALUES (@Version, @Description, @SqlScript, @Success, @ErrorMessage)";

            using var cmd = new SqlCommand(insertSql, conn, transaction);
            cmd.Parameters.AddWithValue("@Version", (object)version ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Description", description);
            cmd.Parameters.AddWithValue("@SqlScript", (object)sql ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Success", success);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object)errorMessage ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private string GenerateCreateTableSql(TableDefinition table)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE [{table.SchemaName}].[{table.TableName}] (");

            var columnDefs = new List<string>();
            var primaryKeys = new List<string>();

            foreach (var col in table.Columns)
            {
                var colDef = GenerateColumnDefinition(col);
                columnDefs.Add($"    {colDef}");

                if (col.IsPrimaryKey)
                {
                    primaryKeys.Add($"[{col.ColumnName}]");
                }
            }

            sb.AppendLine(string.Join(",\n", columnDefs));

            // 主鍵約束
            if (primaryKeys.Any())
            {
                sb.AppendLine($"    ,CONSTRAINT [PK_{table.TableName}] PRIMARY KEY ({string.Join(", ", primaryKeys)})");
            }

            sb.AppendLine(")");

            return sb.ToString();
        }

        private string GenerateColumnDefinition(ColumnDefinition col)
        {
            var sb = new StringBuilder();
            sb.Append($"[{col.ColumnName}] {col.DataType}");

            if (col.IsIdentity)
            {
                sb.Append($" IDENTITY({col.IdentitySeed},{col.IdentityIncrement})");
            }

            sb.Append(col.IsNullable ? " NULL" : " NOT NULL");

            if (!string.IsNullOrEmpty(col.DefaultValue) && !col.IsIdentity)
            {
                sb.Append($" DEFAULT {col.DefaultValue}");
            }

            return sb.ToString();
        }

        private string GenerateAddColumnSql(string tableName, ColumnDefinition column, string schemaName)
        {
            var colDef = GenerateColumnDefinition(column);
            return $"ALTER TABLE [{schemaName}].[{tableName}] ADD {colDef}";
        }

        private string GenerateModifyColumnSql(string tableName, ColumnDefinition column, string schemaName)
        {
            var nullability = column.IsNullable ? "NULL" : "NOT NULL";
            return $"ALTER TABLE [{schemaName}].[{tableName}] ALTER COLUMN [{column.ColumnName}] {column.DataType} {nullability}";
        }

        private string GenerateDropDefaultConstraintSql(string tableName, string columnName, string schemaName)
        {
            // 查詢預設值約束名稱
            const string sql = @"
                SELECT d.name
                FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                JOIN sys.tables t ON c.object_id = t.object_id
                JOIN sys.schemas s ON t.schema_id = s.schema_id
                WHERE s.name = @SchemaName AND t.name = @TableName AND c.name = @ColumnName";

            using var conn = GetConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SchemaName", schemaName);
            cmd.Parameters.AddWithValue("@TableName", tableName);
            cmd.Parameters.AddWithValue("@ColumnName", columnName);

            var constraintName = cmd.ExecuteScalar()?.ToString();
            if (!string.IsNullOrEmpty(constraintName))
            {
                return $"ALTER TABLE [{schemaName}].[{tableName}] DROP CONSTRAINT [{constraintName}]";
            }

            return null;
        }

        #endregion

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                }
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}
