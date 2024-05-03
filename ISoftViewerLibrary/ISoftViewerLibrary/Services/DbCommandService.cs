using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories.Tables;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Services
{
    public class DbCommandService<T> : EntityTableRepository<T>, IDisposable
        where T : ElementAbstract
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="dbUserId"></param>
        /// <param name="dbPassword"></param>
        /// <param name="dbName"></param>
        /// <param name="dbServerName"></param>
        public DbCommandService(string dbUserId, string dbPassword, string dbName, string dbServerName, bool updateBinary = true)
        {
            DBUser = dbUserId;
            DBPassword = dbPassword;
            DBName = dbName;
            ServerName = dbServerName;
            UpdateBinary = updateBinary;

            NewTransaction();            
        }

        #region Fields        
        /// <summary>
        /// Table
        /// </summary>
        protected string TableName;
        /// <summary>
        /// 複合式查詢服務單一工作項目
        /// </summary>
        protected IDBUnitOfWork DbTransactionUnitOfWork;
        /// <summary>
        /// 資料庫使用者名稱
        /// </summary>
        private readonly string DBUser;
        /// <summary>
        /// 資料庫密碼
        /// </summary>
        private readonly string DBPassword;
        /// <summary>
        /// 資料庫名稱
        /// </summary>
        private readonly string DBName;
        /// <summary>
        /// 伺服器名稱
        /// </summary>
        private readonly string ServerName;
        /// <summary>
        /// 是否要更新二進位資料
        /// </summary>
        private readonly bool UpdateBinary;
        #endregion

        #region Methods
        /// <summary>
        /// 建置表格
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="normalKeys"></param>
        /// <returns></returns>
        public DbCommandService<T> BuildTable(string tableName, List<PairDatas> primaryKeys, List<PairDatas> normalKeys, string userid = "Querier")
        {
            CustomizeTableBuilder tableBuilder = new();
            TableElement = tableBuilder.InitTable(userid, tableName)
                .CreatePrimaryKeyFields(primaryKeys)
                .CreateNormalKeyFields(normalKeys)
                .BuildTable();
            return this;
        }
        /// <summary>
        /// 建立新的資料交易
        /// </summary>
        protected void NewTransaction()
        {
            DbTransactionUnitOfWork = new GeneralDatabaseOptUnitOfWork(
                "CompositeAppServiceUnitOfWork", DBUser, DBPassword, DBName, ServerName, true, UpdateBinary);

            DbTransactionUnitOfWork.BeginTransaction();
            DbTransactionUnitOfWork.RegisterRepository(this);
        }
        /// <summary>
        /// 新增或修改
        /// </summary>
        /// <returns></returns>
        public Task<bool> AddOrUpdate(bool newTransaction = false)
        {
            if (newTransaction == true)
                NewTransaction();

            if (TransactionExecutor == null)
                return Task.FromResult(false);
            bool result = default;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    if (result = TableElement.Accept(TransactionExecutor) == true)
                        DbTransactionUnitOfWork.Commit();
                    else
                        DbTransactionUnitOfWork.Rollback();
                }
                catch (Exception)
                {
                    DbTransactionUnitOfWork.Rollback();
                    result = false;
                }
            });
            task.Wait();
            return Task.FromResult(result); ;
        }
        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
