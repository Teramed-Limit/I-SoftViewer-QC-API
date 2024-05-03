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
    #region DbQueriesService
    /// <summary>
    /// 資料庫查詢服務
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbQueriesService<T> : EntityTableRepository<T>, IDisposable
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
        public DbQueriesService(string dbUserId, string dbPassword, string dbName, string dbServerName)
        {
            //UserId = userId;
            DbTransactionUnitOfWork = new GeneralDatabaseOptUnitOfWork(
                "CompositeAppServiceUnitOfWork",
                dbUserId,
                dbPassword,
                dbName, dbServerName,
                false);

            DbTransactionUnitOfWork.BeginTransaction();
            DbTransactionUnitOfWork.RegisterRepository(this);
        }

        #region Fields
        /// <summary>
        /// 使用者帳號
        /// </summary>
        //protected string UserId;
        /// <summary>
        /// Table
        /// </summary>
        protected string TableName;
        /// <summary>
        /// 複合式查詢服務單一工作項目
        /// </summary>
        protected IDBUnitOfWork DbTransactionUnitOfWork;
        #endregion

        #region Methods
        /// <summary>
        /// 查詢函式
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<List<ICommonFieldProperty>>> SearchFunc(Func<T, bool> predicate)
        {
            try
            {
                //每次都要確認是否已建立客製化表格
                PrepareElementSchema();
                //有指定條件
                if (predicate != null)
                    return await GetWhereAsync(predicate) as List<List<ICommonFieldProperty>>;
                    
                //查詢多筆資料
                return await GetAllAsync() as List<List<ICommonFieldProperty>>;
            }
            finally
            {
                DbTransactionUnitOfWork.CloseConnection();
            }
        }
        /// <summary>
        /// 建置表格
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="primaryKeys"></param>
        /// <param name="normalKeys"></param>
        /// <returns></returns>
        public DbQueriesService<T> BuildTable(string tableName, List<PairDatas> primaryKeys, List<PairDatas> normalKeys, string userid = "Querier")
        {
            CustomizeTableBuilder tableBuilder = new();
            TableElement = tableBuilder.InitTable(userid, tableName)
                .CreatePrimaryKeyFields(primaryKeys)
                .CreateNormalKeyFields(normalKeys)
                .BuildTable();
            return this;
        }
        /// <summary>
        /// 取得資料
        /// </summary>
        /// <returns></returns>
        public new T GetData()
        {
            T value = base.GetData();
            DbTransactionUnitOfWork.CloseConnection();
            return value;
        }
        /// <summary>
        /// 新增或修改
        /// </summary>
        /// <returns></returns>
        //public Task<bool> AddOrUpdate()
        //{
        //    if (TransactionExecutor == null)
        //        return Task.FromResult(false); 
        //    bool result = default;
        //    var task = Task.Factory.StartNew(() =>
        //    {
        //        try
        //        {
        //            if (result = TableElement.Accept(TransactionExecutor) == true)
        //                DbTransactionUnitOfWork.Commit();
        //            else
        //                DbTransactionUnitOfWork.Rollback();
        //        }
        //        catch (Exception)
        //        {
        //            DbTransactionUnitOfWork.Rollback();                    
        //            result = false;
        //        }
        //    });
        //    task.Wait();
        //    return Task.FromResult(result); ;
        //}
        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion        
    }
    #endregion
}
