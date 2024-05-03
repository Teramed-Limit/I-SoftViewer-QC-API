using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Utils;

namespace ISoftViewerLibrary.Services.RepositoryService.Interface
{
    #region ICommonRepositoryService

    /// <summary>
    ///     應用層服務介面
    /// </summary>
    public interface ICommonRepositoryService<T>
    {
        IEnumerable<T> GetAll();
        IEnumerable<T> Get(string id);
        IEnumerable<T> Get(List<PairDatas> where, List<PairDatas> normalKeys = null);
        IEnumerable<T> GetSpecifyColumn(List<PairDatas> where, List<PairDatas> normalKeys);
        bool AddOrUpdate(T data, string identityUserId);
        bool AddOrUpdate(TableField tableField, string identityUserId);
        bool Insert(T data, string identityUserId);
        bool Delete(string id);
        void GenerateNewTransaction();
    }

    public abstract class CommonRepositoryService<T> : ICommonRepositoryService<T> where T : IJsonDataset, new()
    {
        protected PacsDBOperationService DbOperator;

        // 主鍵
        protected string PrimaryKey;

        // 外來鍵
        protected string RelatedTablePrimaryKey;

        // 用Identity當作主鍵
        protected bool IsIdentityPrimaryKey = false;

        // 資料表名稱
        protected string TableName;

        protected CommonRepositoryService(string tableName, PacsDBOperationService dbOperator)
        {
            DbOperator = dbOperator;
            TableName = tableName;
        }

        /// <summary>
        ///     查詢資料表所有資料
        /// </summary>
        public virtual IEnumerable<T> GetAll()
        {
            return DbOperator
                .BuildQueryTable(TableName)
                .Query<T>();
        }

        /// <summary>
        ///     主鍵為查詢條件之資料
        /// </summary>
        public virtual IEnumerable<T> Get(string id)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = id }
            };

            return DbOperator
                .BuildQueryTable(TableName, primaryKeys, new List<PairDatas>())
                .Query<T>();
        }

        /// <summary>
        ///     客製化查詢條件之資料
        /// </summary>
        public virtual IEnumerable<T> Get(List<PairDatas> where, List<PairDatas> normalKeys = null)
        {
            normalKeys ??= new List<PairDatas>();
            return DbOperator
                .BuildQueryTable(TableName, where, normalKeys)
                .Query<T>();
        }
        
        /// <summary>
        ///     客製化查詢條件之資料
        /// </summary>
        public virtual IEnumerable<T> GetSpecifyColumn(List<PairDatas> where, List<PairDatas> normalKeys)
        {
            normalKeys ??= new List<PairDatas>();
            return DbOperator
                .BuildNoneQueryTable(TableName, where, normalKeys)
                .Query<T>();
        }

        /// <summary>
        ///     新增修改資料
        /// </summary>
        public virtual bool AddOrUpdate(T data, string identityUserId = null)
        {
            var tableField = ObjectToPairDatas.Convert(data, new List<string> { PrimaryKey }, IsIdentityPrimaryKey ? PrimaryKey : null);
            return DbOperator
                .BuildNoneQueryTable(TableName, tableField.PrimaryFields, tableField.NormalFields, identityUserId)
                .AddOrUpdate();
        }

        /// <summary>
        ///     新增修改資料
        /// </summary>
        public virtual bool AddOrUpdate(TableField tableField, string identityUserId = null)
        {
            return DbOperator
                .BuildNoneQueryTable(TableName, tableField.PrimaryFields, tableField.NormalFields, identityUserId)
                .AddOrUpdate();
        }

        /// <summary>
        ///     新增資料
        /// </summary>
        public bool Insert(T data, string identityUserId)
        {
            var tableField = ObjectToPairDatas.Convert(data, new List<string>(), IsIdentityPrimaryKey ? PrimaryKey : null);
            return DbOperator
                .BuildNoneQueryTable(TableName, tableField.PrimaryFields, tableField.NormalFields, identityUserId)
                .Insert();
        }

        /// <summary>
        ///     刪除條件為主鍵資料
        /// </summary>
        public virtual bool Delete(string id)
        {
            var primaryKeys = new List<PairDatas>
            {
                new() { Name = PrimaryKey, Value = id }
            };

            return DbOperator
                .BuildNoneQueryTable(TableName, primaryKeys, new List<PairDatas>())
                .Remove();
        }

        /// <summary>
        ///     當一個Request有多個查詢時適用
        /// </summary>
        public virtual void GenerateNewTransaction()
        {
            DbOperator.GenerateUnitOfWork();
        }
    }
    #endregion
}