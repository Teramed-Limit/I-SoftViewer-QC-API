using ISoftViewerLibrary.Models.DatabaseOperator;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.UnitOfWorks
{
    #region GeneralDatabaseOptUnitOfWork
    /// <summary>
    /// 負責資料庫溝通的單一工作樣式
    /// </summary>
    public class GeneralDatabaseOptUnitOfWork : IDBUnitOfWork, IUnitOfWorkRepository
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="transactionName"></param>
        /// <param name="enabledTransaction"></param>
        /// <param name="supportBinaryEexecutor"></param>
        public GeneralDatabaseOptUnitOfWork(string transactionName, bool enabledTransaction = true, bool supportBinaryEexecutor = true)
        {
            IsDBConnected = false;
            NameOfWork = transactionName;
            DBUserId = "";
            DBPassword = "";
            DBName = "";
            DBServerName = "";
            EnabledDBTransaction = enabledTransaction;
            SupportBinaryExecutor = supportBinaryEexecutor;
            OptMessage = "";            
            //資料來源準備及連線 MOD BY JB 20210416 預設不做資料庫連線
            //DataSourceConnection();
        }
        /// <summary>
        /// 帶有資料庫連線資訊的建構
        /// </summary>
        /// <param name="transactionName"></param>
        /// <param name="dbUserId"></param>
        /// <param name="dbPassword"></param>
        /// <param name="dbName"></param>
        /// <param name="dbServerName"></param>
        /// <param name="enabledTransaction"></param>
        /// <param name="supportBinaryEexecutor"></param>
        public GeneralDatabaseOptUnitOfWork(string transactionName, string dbUserId, string dbPassword, string dbName, string dbServerName,
            bool enabledTransaction = true, bool supportBinaryEexecutor = true)
        {
            NameOfWork = transactionName;

            DBUserId = dbUserId;
            DBPassword = dbPassword;
            DBName = dbName;
            DBServerName = dbServerName;
            EnabledDBTransaction = enabledTransaction;
            SupportBinaryExecutor = supportBinaryEexecutor;
            OptMessage = "";
            //資料來源準備及連線 MOD BY JB 20210416 預設不做資料庫連線
            //DataSourceConnection();
        }

        #region Fields
        /// <summary>
        /// 客製化表格的執行器
        /// </summary>
        public IExecutorInterface CustomizeTableEexecutor { protected get; set; }
        /// <summary>
        /// 更新資料庫引擎
        /// </summary>
        public IExecutorInterface TransactionContextEngine { protected get; set; }
        /// <summary>
        /// 查詢資料庫引擎
        /// </summary>
        public IExecutorInterface SelectContextEngine { protected get; set; }
        /// <summary>
        /// 刪除資料庫引擎
        /// </summary>
        public IExecutorInterface DeleteTableExecutor { protected get; set; }
        /// <summary>
        /// 刪除資料庫引擎
        /// </summary>
        public IExecutorInterface InsertTableExecutor { protected get; set; }
        /// <summary>
        /// 資料庫是否已連線
        /// </summary>
        public bool IsDBConnected { get; protected set; }
        /// <summary>
        /// 處理訊息
        /// </summary>
        public string OptMessage { get; protected set; }
        /// <summary>
        /// ADO.Net SQL Connection
        /// </summary>
        protected SqlConnection DBConnection = null;
        /// <summary>
        /// 資料庫交易控管
        /// </summary>
        protected SqlTransaction DBTransaction = null;
        /// <summary>
        /// 單一工作的識別名稱
        /// </summary>
        protected string NameOfWork;
        /// <summary>
        /// 資料庫使用者ID
        /// </summary>
        internal string DBUserId;
        /// <summary>
        /// 資料庫密碼
        /// </summary>
        internal string DBPassword;
        /// <summary>
        /// 資料庫名稱
        /// </summary>
        internal string DBName;
        /// <summary>
        /// 資料庫主機名稱
        /// </summary>
        internal string DBServerName;
        /// <summary>
        /// 啟用資料庫交易機制
        /// </summary>
        internal bool EnabledDBTransaction;
        /// <summary>
        /// 是否支援二進位欄位的執行器
        /// </summary>
        internal bool SupportBinaryExecutor;
        #endregion

        #region Methods
        /// <summary>
        /// 資料來源連線
        /// </summary>
        /// <param name="repository"></param>
        public void RegisterRepository(IUnitOfWorkRepository repository)
        {
            repository.TransactionContextEngine = TransactionContextEngine;
            repository.SelectContextEngine = SelectContextEngine;
            repository.CustomizeTableEexecutor = CustomizeTableEexecutor;
            repository.DeleteTableExecutor = DeleteTableExecutor;
            repository.InsertTableExecutor = InsertTableExecutor;
        }
        /// <summary>
        /// 關始進行資料庫交易
        /// </summary>
        public virtual void BeginTransaction()
        {
            try
            {
                //建立資料庫連線引撆
                if (DBConnection == null)
                    DBConnection = new SqlConnection(SQLConnectionStringBuilding());

                if (DBConnection.State != System.Data.ConnectionState.Open)
                {
                    DBConnection.Open();
                    //若有需要做資料異動,才需要建立資料庫交易控管物件
                    if (EnabledDBTransaction)
                    {
                        //每次的交易都產生新的Transaction  
                        Random rnd = new Random();
                        int randomFileName = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
                        DBTransaction = DBConnection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted, Convert.ToString(randomFileName));
                    }                    
                    //SQL語法執行器
                    TransactionContextEngine = new SqlCompositeExecutor(DBConnection, DBTransaction, null, SupportBinaryExecutor);
                    SelectContextEngine = new SqlSelectMultiDataCmdExecutor(DBConnection, DBTransaction, null);
                    //預先放入一組執行器,任何交易之前要先判斷該表格是否已經定義好表格(不需要交易,只單純取表格欄位屬性)
                    CustomizeTableEexecutor = new SqlGetSchemaAdvanced2CmdExecutor(DBConnection, DBTransaction, null);
                    //刪除資料執行器
                    DeleteTableExecutor = new SqlDeleteCmdExecutor(DBConnection, DBTransaction, null);
                    //新增資料執行器
                    InsertTableExecutor = new SqlInsertCmdExecutor(DBConnection, DBTransaction, null);
                }
            }
            catch (Exception ex)
            {
                DBConnection.Close();
                OptMessage = ex.Message;
            }
        }
        /// <summary>
        /// 確定寫回資料庫
        /// </summary>
        public void Commit()
        {
            //有資料庫交易機制,才需要Commit
            if (DBTransaction == null)
                return;
            DBTransaction.Commit();
            //ADD 20210522 Oscar 關閉連線
            DBConnection.Close();
        }
        /// <summary>
        /// 回復資料更新資料
        /// </summary>
        public void Rollback()
        {
            //有資料庫交易機制,才需要Commit
            if (DBTransaction == null)
                return;
            //有問題,則需要把資料做回復
            DBTransaction.Rollback();
            //ADD 20210522 Oscar 關閉連線
            DBConnection.Close();
        }
        /// <summary>
        /// 結束資料庫連線
        /// </summary>
        public void CloseConnection()
        {
            DBConnection.Close();
        }
        /// <summary>
        /// 組合資料庫連接字串
        /// </summary>
        /// <returns></returns>
        protected virtual string SQLConnectionStringBuilding()
        {
            string result = "";            
            SqlConnectionStringBuilder builer = new SqlConnectionStringBuilder();
            try
            {
                builer.DataSource = DBServerName.Trim();
                builer.InitialCatalog = DBName.Trim();
                builer.UserID = DBUserId.Trim();
                builer.Password = DBPassword.Trim();
                result = builer.ConnectionString;
            }
            catch (Exception ex)
            {
                IsDBConnected = false;
                OptMessage = ex.Message;
            }
            return result;
        }
        /// <summary>
        /// 資料庫連線
        /// </summary>
        /// <returns></returns>
        public bool DataSourceConnection()
        {
            //ADD BY JB 20210416 預設不做資料庫連線,在需要使用時,才做資料庫連線
            if (DBConnection != null)
                return IsDBConnected;

            IsDBConnected = true;
            try
            {
                //建立資料庫連線引撆
                DBConnection = new SqlConnection(SQLConnectionStringBuilding());
                DBConnection.Open();
                //若有需要做資料異動,才需要建立資料庫交易控管物件
                if (EnabledDBTransaction == true)
                {
                    //每次的交易都產生新的Transaction  
                    Random rnd = new Random();
                    int randomFileName = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
                    DBTransaction = DBConnection.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted, Convert.ToString(randomFileName));
                }
                //SQL語法執行器
                TransactionContextEngine = new SqlCompositeExecutor(DBConnection, DBTransaction, null, SupportBinaryExecutor);
                SelectContextEngine = new SqlSelectMultiDataCmdExecutor(DBConnection, DBTransaction, null);
                //定義表格執行器
                CustomizeTableEexecutor = new SqlGetSchemaAdvancedCmdExecutor(DBConnection, DBTransaction, null);
                //刪除資料執行器
                DeleteTableExecutor = new SqlDeleteCmdExecutor(DBConnection, DBTransaction, null);
            }
            catch (Exception ex)
            {
                IsDBConnected = false;
                OptMessage = ex.Message;
            }
            return IsDBConnected;
        }
        #endregion
    }
    #endregion
}