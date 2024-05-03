using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Data.SqlClient;
// compile with: /reference:System.Data.SqlDbType=C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0\System.Data.Common.dll
using System.Data;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlCommandExecutor 
    /// <summary>
    /// 資料庫查詢專用操作物件
    /// </summary>
    public class SqlCommandExecutor : IExecutorInterface
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SqlCommandExecutor(SqlConnection connection, SqlTransaction sqlTransaction, SqlCommandExecutor sqlExecutor)
        {
            SQLConnection = connection;            
            SQLCmd = SQLConnection.CreateCommand();
            SQLExecutor = sqlExecutor;
            if (sqlTransaction != null)
            {
                SQLTransaction = sqlTransaction;
                SQLCmd.Transaction = SQLTransaction;
            }
            Result = 0;
            DbMessages = "      -- Database executed successful !!";
        }

        #region Fields
        /// <summary>
        /// ADO.NET的查詢元件
        /// </summary>
        protected SqlConnection SQLConnection;        
        /// <summary>
        /// 保留一組可以額外執行的資料庫操作動作
        /// </summary>
        protected SqlCommandExecutor SQLExecutor;
        /// <summary>
        /// 資料庫SQL語法執行元件
        /// </summary>
        protected SqlCommand SQLCmd;
        /// <summary>
        /// 資料庫控制器
        /// </summary>
        protected SqlTransaction SQLTransaction;
        /// <summary>
        /// 執行結果
        /// </summary>
        protected int Result;
        /// <summary>
        /// 資料庫相關訊息
        /// </summary>
        protected string DbMessages;
        #endregion

        #region Methods
        /// <summary>
        /// 執行資料庫操作
        /// </summary>
        /// <param name="element"></param>
        public virtual bool Execute(IElementInterface element, object condition = null)
        {
            if (SQLConnection == null)
                return false;
            //沒有上層元件就不處理,表示為Root元件
            if (SQLExecutor == null)
                return true;

            return SQLExecutor.Execute(element);
        }
        /// <summary>
        /// 執行結果
        /// </summary>
        /// <returns></returns>
        public int ExecuteResult()
        {
            return Result;
        }
        /// <summary>
        /// 執行過程中是否有任何訊息需要記錄
        /// </summary>
        /// <returns></returns>
        public string Messages()
        {
            return DbMessages;
        }
        /// <summary>
        /// 執行非查詢語法
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual bool SqlExecuteNonQuery(string sql)
        {
            try
            {
                //執行SQL查詢
                if (SQLConnection.State == System.Data.ConnectionState.Closed)
                    SQLConnection.Open();
                //記錄新增了多少筆資料
                SQLCmd.CommandText = sql;
                Result = SQLCmd.ExecuteNonQuery();
                //SQLConnection.Close();
            }
            catch (SystemException e)
            {
                Serilog.Log.Error(e, "SQLExecuteNonQuery Error");
                DbMessages = e.Message;
                return false;
            }
            return true;
        }
        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// 組合欄位名稱 MOD BY JB 20210125 加入N前綴字,避免亂碼問題
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <param name="frontQuotedString"></param>
        /// <param name="rearQuotedString"></param>
        /// <param name="conjunction"></param>
        /// <returns></returns>
        protected static string MakeSqlValue(string value, string name, string frontQuotedString, string rearQuotedString, string conjunction = ", ")
        {
            if (value.Trim() != string.Empty)
                value += conjunction;
            //MOD BY JB 20210122 要避免單引號組合SQL語法造成的問題
            return value + frontQuotedString + name.Replace("'", "''") + rearQuotedString;
        }
        /// <summary>
        /// 新增參數到SQLCommand的Parameters中
        /// </summary>
        /// <param name="commonField"></param>
        protected void AddParameters(ICommonFieldProperty commonField)
        {
            string fieldName = "@" + commonField.FieldName;
            if (commonField.Type == FieldType.ftString || commonField.Type == FieldType.ftDateTime)
            {
                //MOD BY JB 20210427 Trim()
                if(string.IsNullOrEmpty(commonField.Value))
                {
                    SQLCmd.Parameters.AddWithValue(fieldName, DBNull.Value);
                }
                else
                {
                    string fixString = commonField.Value.Replace("'", "''").Trim();
                    SQLCmd.Parameters.AddWithValue(fieldName, fixString);
                }
                // string fixString = commonField.Value.Replace("'", "''").Trim();
                //SQLCmd.Parameters.Add(fieldName, SqlDbType.NVarChar).Value = fixString;
                //方法二
                // SQLCmd.Parameters.AddWithValue(fieldName, fixString);
            }
            if (commonField.Type == FieldType.ftInt)
            {
                int value = int.TryParse(commonField.Value, out int intValue) ? intValue : 0;
                //SQLCmd.Parameters.Add(fieldName, SqlDbType.Int).Value = int.TryParse(commonField.Value, out int intValue) ? intValue : 0;
                SQLCmd.Parameters.AddWithValue(fieldName, value);
            }
            if (commonField.Type == FieldType.ftBinary)
            {
                //SQLCmd.Parameters.Add(fieldName,SqlDbType.VarBinary).Value = commonField.BinaryValue;
                SQLCmd.Parameters.AddWithValue(fieldName, commonField.BinaryValue);
            }            
        }
        #endregion
    }
    #endregion    
}