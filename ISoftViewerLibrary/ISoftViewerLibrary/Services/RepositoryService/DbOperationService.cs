using System;
using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Logic.Converter;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories.Tables;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Services.RepositoryService
{
    #region DbOperationService

    /// <summary>
    /// 獨立一個應用層服務,客製化查詢
    /// </summary>
    public class DbOperationService<T> : EntityTableRepository<T>, IDisposable where T : ElementAbstract
    {
        #region Fields

        /// <summary>
        /// 複合式查詢服務單一工作項目
        /// </summary>
        protected IDBUnitOfWork DbTransactionUnitOfWork;
        #endregion

        #region Methods

        public void GenerateUnitOfWork(string dbUserId, string dbPassword, string dbName, string dbServerName)
        {
            DbTransactionUnitOfWork = new GeneralDatabaseOptUnitOfWork(
                "DbOperationServiceUnitOfWork",
                dbUserId,
                dbPassword,
                dbName,
                dbServerName);

            DbTransactionUnitOfWork.BeginTransaction();
            DbTransactionUnitOfWork.RegisterRepository(this);
        }

        public List<List<ICommonFieldProperty>> SearchFunc(Func<T, bool> predicate)
        {
            try
            {
                //每次都要確認是否已建立客製化表格
                PrepareElementSchema();
                if (predicate != null)
                    //有指定條件
                    return GetWhere(predicate) as List<List<ICommonFieldProperty>>;
                //查詢多筆資料
                return GetAll() as List<List<ICommonFieldProperty>>;
            }
            finally
            {
                DbTransactionUnitOfWork.CloseConnection();
            }

        }

        public DbOperationService<T> BuildQueryTable(
            string tableName,
            List<PairDatas> primaryKeys = null,
            List<PairDatas> normalKeys = null,
            string userId = null)
        {
            CustomizeTableBuilder tableBuilder = new CustomizeTableBuilder();

            //建立表格
            TableElement = tableBuilder
                .InitTable(userId, tableName)
                .BuildTable();

            //建立資料表格欄位
            PrepareElementSchema();

            if (primaryKeys != null || normalKeys != null)
            {
                TableElement = tableBuilder
                .CreatePrimaryKeyFields(primaryKeys)
                .CreateNormalKeyFields(normalKeys)
                .BuildTable();
            }

            return this;
        }

        public DbOperationService<T> BuildNoneQueryTable(string tableName, List<PairDatas> primaryKeys, List<PairDatas> normalKeys, string userId = null)
        {
            CustomizeTableBuilder tableBuilder = new CustomizeTableBuilder();

            //建立表格
            TableElement = tableBuilder
                .InitTable(userId, tableName)
                .CreatePrimaryKeyFields(primaryKeys)
                .CreateNormalKeyFields(normalKeys)
                .BuildTable();

            return this;
        }

        public T Query(bool commit = true)
        {
            var value = GetData();
            if (commit)
                DbTransactionUnitOfWork.CloseConnection();
            return value;
        }

        public IEnumerable<R> Query<R>(bool commit = true) where R : IJsonDataset, new()
        {
            var table = GetData();

            List<R> results = new List<R>();
            table.DBDatasets.ForEach(dataset =>
            {
                var jsonData = new R();
                new CommonFieldProperties2JSONConverter().Convert(dataset, jsonData);
                results.Add(jsonData);
            });

            if (commit)
                DbTransactionUnitOfWork.CloseConnection();

            return results.AsEnumerable();
        }

        public bool AddOrUpdate()
        {
            if (TransactionExecutor == null)
                return false;

            if (TableElement.Accept(TransactionExecutor) == false)
            {
                DbTransactionUnitOfWork.Rollback();
                return false;
            }
            DbTransactionUnitOfWork.Commit();
            return true;
        }


        public bool Insert()
        {
            if (TableElement == null)
                return false;
            try
            {
                //容器要有欄位
                if (TableElement.DBNormalFields.Count <= 0)
                    throw new Exception("Can not insert data without fields");

                if (!TableElement.Accept(new List<IExecutorInterface>() { InsertTableExecutor }))
                {
                    DbTransactionUnitOfWork.Rollback();
                    return false;
                }
                DbTransactionUnitOfWork.Commit();
                return true;
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Insert data failed");
                return false;
            }
        }

        public bool Remove(bool commit = true)
        {
            if (TableElement == null || TransactionExecutor == null)
                return false;
            try
            {
                //目前不允許刪除整個資料表格內容,避免資料表全部誤刪
                if (TableElement.DBPrimaryKeyFields.Find(field => field.Value != "") == null)
                    throw new Exception("Can not delete all data from table");

                if (TableElement.Accept(new List<IExecutorInterface>() { DeleteTableExecutor }) == false)
                {
                    DbTransactionUnitOfWork.Rollback();
                    return false;
                }
                if (commit)
                    DbTransactionUnitOfWork.Commit();
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    #endregion
}