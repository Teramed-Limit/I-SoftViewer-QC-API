using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region DatabaseConnection
    /// <summary>
    /// 資料庫連線Singleton
    /// </summary>
    public class DatabaseConnection
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbConnString"></param>
        private DatabaseConnection(string dbConnString)
        {
            DBConnectionString = dbConnString;
        }
        /// <summary>
        /// 資料庫連接字串
        /// </summary>
        private readonly string DBConnectionString;
        /// <summary>
        /// SQL Server SqlConnection
        /// </summary>
        private static SqlConnection SQLConnection = null;
        /// <summary>
        /// 鎖定物件
        /// </summary>
        private static readonly object LockObject = new();
        /// <summary>
        /// 整個系統皆使用同一個資料庫連線
        /// </summary>
        /// <param name="connString"></param>
        /// <returns></returns>
        public static SqlCommand CreateSqlCommand(string connString)
        {
            SqlCommand command = null;
            if (SQLConnection == null)
            {
                lock (LockObject)
                {
                    if (SQLConnection == null)
                        SQLConnection = new SqlConnection(connString);

                    command = SQLConnection.CreateCommand();
                }
            }
            return command;
        }
    }
    #endregion
}