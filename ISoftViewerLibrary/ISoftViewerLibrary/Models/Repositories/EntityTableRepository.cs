using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace ISoftViewerLibrary.Models.Repositories.Tables
{
    #region EntityTableRepository
    /// <summary>
    /// 實作存取資料表的處理庫
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityTableRepository<T> : IEntityRespository<T>
        where T : class, IElementInterface
    {
        /// <summary>
        /// 建構
        /// </summary>
        public EntityTableRepository()
        {
            TableElement = null;
            TransactionExecutor = new List<IExecutorInterface>();
            SelectExecutor = new List<IExecutorInterface>();
            GetSchemaExecutor = new List<IExecutorInterface>();
        }

        #region Fields
        /// <summary>
        /// 資料表物件
        /// </summary>
        public IElementInterface TableElement { get; set; }
        /// <summary>
        /// 資料庫更新引擎
        /// </summary>
        protected IExecutorInterface _TransactionContextEngine;
        /// <summary>
        /// 資料庫查詢引擎
        /// </summary>
        protected IExecutorInterface _SelectContextEngine;
        /// <summary>
        /// 客製化表格的執行器,原則上只會有一組
        /// </summary>
        public IExecutorInterface _CustomizeTableEexecutor;
        /// <summary>
        /// 交易執行器
        /// </summary>
        protected List<IExecutorInterface> TransactionExecutor;
        /// <summary>
        /// 查詢執行器
        /// </summary>
        protected List<IExecutorInterface> SelectExecutor;
        /// <summary>
        /// 客製化表格欄位執行器
        /// </summary>
        protected List<IExecutorInterface> GetSchemaExecutor;
        /// <summary>
        /// 資料庫交易執行器
        /// </summary>
        public virtual IExecutorInterface TransactionContextEngine
        {
            set
            {
                _TransactionContextEngine = (IExecutorInterface)value;
                TransactionExecutor.Clear();
                TransactionExecutor.Add(_TransactionContextEngine);
            }
        }
        /// <summary>
        /// 資料庫查詢執行器
        /// </summary>
        public virtual IExecutorInterface SelectContextEngine
        {
            set
            {
                _SelectContextEngine = (IExecutorInterface)value;
                SelectExecutor.Clear();
                SelectExecutor.Add(_SelectContextEngine);
            }
        }
        /// <summary>
        /// 客製化表格執行器
        /// </summary>
        public virtual IExecutorInterface CustomizeTableEexecutor
        {
            set
            {
                _CustomizeTableEexecutor = (IExecutorInterface)value;
                GetSchemaExecutor.Clear();
                GetSchemaExecutor.Add(_CustomizeTableEexecutor);
            }
        }
        /// <summary>
        /// 刪除表格資料的執行器
        /// </summary>
        public IExecutorInterface DeleteTableExecutor { protected get; set; }
        /// <summary>
        /// 刪除表格資料的執行器
        /// </summary>
        public IExecutorInterface InsertTableExecutor { protected get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// 依照名稱取得實體資料表格裡的資料
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetData()
        {
            if (TableElement == null)
                return default;
            if (TableElement.Accept(SelectExecutor) == false)
                return default;

            return (T)TableElement;
        }
        /// <summary>
        ///  取得所有實體資料
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<ICommonFieldProperty>> GetAll()
        {
            //先清空之前查詢的資料
            TableElement.ClearWholeFieldValues();
            //清空條件,重新查詢一次後,在取得所有資料
            if (GetData() == null)
                return null;

            List<List<ICommonFieldProperty>> datasets = new();
            TableElement.DBDatasets.ForEach(x => { datasets.Add(x); });

            return datasets;
        }
        /// <summary>
        /// 依照條件取得實體資料
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<List<ICommonFieldProperty>> GetWhere(Func<T, bool> predicate)
        {
            //查詢前,先清除查詢條件
            TableElement.ClearWholeFieldValues();
            if (predicate((T)TableElement) == false)
                return null;

            return GetData()?.DBDatasets;
        }
        /// <summary>
        /// 取得筆有資料筆數
        /// </summary>
        /// <returns></returns>
        public int CountAll()
        {
            //清空條件,重新查詢一次後,在取得所有資料
            if (GetData() == null)
                return 0;

            return TableElement.DBDatasets.Count;
        }
        /// <summary>
        /// 新增及修改實體資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool AddOrUpdate(T entity)
        {
            if (TransactionExecutor == null)
                return false;

            if (entity.Accept(TransactionExecutor) == false)
                return false;

            return true;
        }
        /// <summary>
        /// 取得內部的元素實例
        /// </summary>
        /// <returns></returns>
        public T CloneElementInstance()
        {
            return TableElement.Clone() as T;
        }
        /// <summary>
        /// 由於要支援客製表格且也要可以做Where查詢,所以提供Reset Table Schema的功能
        /// </summary>
        /// <returns></returns>
        public bool PrepareElementSchema()
        {
            if (TableElement == null)
                return false;
            //先取得客製化表格的欄位屬性
            try
            {
                TableElement.ClearFields();
                TableElement.Accept(GetSchemaExecutor);
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 移除資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Remove(T entity)
        {
            if (TableElement == null)
                return false;
            try
            {
                //目前不允許刪除整個資料表格內容,避免資料表全部誤刪
                if (TableElement.DBPrimaryKeyFields.Find(field => field.Value != "") == null)
                    throw new Exception("Can not delete all data from table");

                //執行刪除資料
                TableElement.Accept(new List<IExecutorInterface>() { DeleteTableExecutor });
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Insert(T entity)
        {
            if (TableElement == null)
                return false;
            try
            {
                //容器要有欄位
                if (TableElement.DBNormalFields.Count <= 0)
                    throw new Exception("Can not insert data without fields");

                //執行新增資料
                return TableElement.Accept(new List<IExecutorInterface>() { InsertTableExecutor });
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                return false;
            }
        }
        /// <summary>
        /// 取得資料,非同步版本
        /// </summary>
        /// <returns></returns>
        public Task<T> GetDataAsync()
        {
            if (TableElement == null)
                return Task.FromResult<T>(default);

            bool resultOfAccept = false;
            var task = Task.Factory.StartNew(() =>
            {
                resultOfAccept = TableElement.Accept(SelectExecutor);
            });
            task.Wait();

            if (resultOfAccept == false)
                return Task.FromResult<T>(default);
            return Task.FromResult((T)TableElement);
        }
        /// <summary>
        /// 取得所有實體資料,非同步版本
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<List<ICommonFieldProperty>>> GetAllAsync()
        {
            List<List<ICommonFieldProperty>> datasets = null;
            var task = Task.Factory.StartNew(() =>
            {
                //先清空之前查詢的資料
                TableElement.ClearWholeFieldValues();
                //清空條件,重新查詢一次後,在取得所有資料
                if (GetData() != null)
                {
                    List<List<ICommonFieldProperty>> datasets = new();
                    TableElement.DBDatasets.ForEach(x => { datasets.Add(x); });
                }
            });
            task.Wait();

            return Task.FromResult<IEnumerable<List<ICommonFieldProperty>>>(datasets);
        }
        /// <summary>
        /// 依照條件取得資料
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public Task<IEnumerable<List<ICommonFieldProperty>>> GetWhereAsync(Func<T, bool> predicate)
        {
            List<List<ICommonFieldProperty>> datasets = null;
            var task = Task.Factory.StartNew(() =>
            {
                //查詢前,先清除查詢條件
                TableElement.ClearWholeFieldValues();
                if (predicate((T)TableElement) == true)
                    datasets = GetData()?.DBDatasets;
            });
            task.Wait();

            return Task.FromResult<IEnumerable<List<ICommonFieldProperty>>>(datasets);
        }
        /// <summary>
        /// 取得筆有資料筆數
        /// </summary>
        /// <returns></returns>
        public Task<int> CountAllAsync()
        {
            int result = default;
            var task = Task.Factory.StartNew(() =>
            {
                //清空條件,重新查詢一次後,在取得所有資料
                if (GetData() == null)
                    result = 0;
                else
                    result = TableElement.DBDatasets.Count;
            });
            task.Wait();

            return Task.FromResult(result);
        }
        /// <summary>
        /// 新增及修改實體資料
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<bool> AddOrUpdateAsync(T entity)
        {
            if (TransactionExecutor == null)
                return Task.FromResult(false);

            bool result = default;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    result = entity.Accept(TransactionExecutor);
                }
                catch (Exception)
                {
                    result = false;
                }
            });
            return Task.FromResult(result);
        }
        /// <summary>
        /// 移除資料,非同步版本
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<bool> RemoveAsync(T entity)
        {
            if (TableElement == null)
                return Task.FromResult(false);
            bool result = default;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    //目前不允許刪除整個資料表格內容,避免資料表全部誤刪
                    if (TableElement.DBPrimaryKeyFields.Find(field => field.Value != "") == null)
                        throw new Exception("Can not delete all data from table");
                    //執行刪除資料
                    result = TableElement.Accept(new List<IExecutorInterface>() { DeleteTableExecutor });
                }
                catch (Exception)
                {
                    result = false;
                }
            });
            task.Wait();

            return Task.FromResult(result);
        }
        /// <summary>
        /// 新增,非同步版本
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public Task<bool> InsertAsync(T entity)
        {
            if (TableElement == null)
                return Task.FromResult(false);

            bool result = default;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    //容器要有欄位
                    if (TableElement.DBNormalFields.Count <= 0)
                        throw new Exception("Can not insert data without fields");

                    //執行新增資料
                    result = TableElement.Accept(new List<IExecutorInterface>() { InsertTableExecutor });
                }
                catch (System.Exception)
                {
                    result = false;
                }
            });
            task.Wait();

            return Task.FromResult(result);
        }
        #endregion
    }
    #endregion

    #region CustomizeTableRepository
    /// <summary>
    /// 主要用來給Customize Table New and Modify來做查詢使用
    /// </summary>
    public class CustomizeTableRepository : EntityTableRepository<CustomizeTable>, IDisposable
    {
        public CustomizeTableRepository(string repositoryName, string tableName)
            : base()
        {
            // TableElement = new CustomizeTable(repositoryName, tableName);
            TableElement = repositoryName == null ? new CustomizeTable(tableName) : new CustomizeTable(repositoryName, tableName);
        }

        #region Methods
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

    #region MasterDetailTableRepository
    /// <summary>
    /// Master/Detail處理庫
    /// </summary>
    public class MasterDetailTableRepository : EntityTableRepository<MasterDetailTable>, IDisposable
    {
        public MasterDetailTableRepository(string repositoryName, string tableName, bool createTableElement)
            : base()
        {
            if (createTableElement == true)
                TableElement = new MasterDetailTable(repositoryName, tableName);
        }

        #region Methods
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