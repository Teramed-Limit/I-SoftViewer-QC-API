using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Services.RepositoryService;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Services
{
    #region DbTableService<T>
    /// <summary>
    /// 資料庫儲存庫服務輔助物件
    /// </summary>
    public class DbTableService<T> : CommonRepositoryService<T>
        where T : IJsonDataset, new()
    {
        public DbTableService(PacsDBOperationService dbOperator)
            : base(DbServiceHelper<T>.GetTableName(), dbOperator)
        {
            PrimaryKey = DbServiceHelper<T>.GetPrimaryKeyName();
            RelatedTablePrimaryKey = DbServiceHelper<T>.GetRelatedTablePrimaryKey();
        }        
    }
    #endregion

    #region TableMappingData
    /// <summary>
    /// 物件及欄位對照表
    /// </summary>
    public class TableMappingData
    {
        /// <summary>
        /// 資料表名稱
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 資料表主鍵欄位
        /// </summary>
        public string PrimaryKey { get; set; }
        /// <summary>
        /// 資料表連外鍵欄位
        /// </summary>
        public string RelatedTablePrimaryKey { get; set; }
    }
    #endregion

    #region DbServiceHelper<T>
    /// <summary>
    /// CommonRepositoryService輔助物件
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DbServiceHelper<T>
    {
        #region Fields
        /// <summary>
        /// 資料表字典
        /// </summary>
        public static readonly Dictionary<string, TableMappingData> DbMappingTables = new()
        {
            { "SvrDcmNodeDb", new TableMappingData() { TableName = "DicomNodes", PrimaryKey = "Name" } },
            { "SvrDcmProviderDb", new TableMappingData() { TableName = "DicomServiceProvider", PrimaryKey = "Name" } },
            { "SvrDcmDestNode", new TableMappingData() { TableName = "DicomDestinationNodes", PrimaryKey = "LogicalName" } },
            { "SvrConfiguration", new TableMappingData() { TableName = "SystemConfiguration", PrimaryKey = "Name" } },
            { "SvrConfigurationsV2", new TableMappingData() { TableName = "SystemConfig", PrimaryKey = "SysConfigName" } },
            { "SvrFileStorageDevice", new TableMappingData() { TableName = "StorageDevice", PrimaryKey = "StorageDeviceID" } },
            { "SvrDcmTags", new TableMappingData() { TableName = "DicomTags", PrimaryKey = "IdentifyName" } },
            { "SvrDcmTagFilters", new TableMappingData() { TableName = "DicomTagFilterDetail", PrimaryKey = "TagFilterName" } },
            { "SvrDcmTagFilterDetail", new TableMappingData() { TableName = "DicomTagFilters", PrimaryKey = "TagFilterName" } },
            { "JobOptResultLog", new TableMappingData() { TableName = "JobOptResultLog", PrimaryKey = "Date" } },
        };        
        #endregion

        #region Methods
        /// <summary>
        /// 取得資料表名稱
        /// </summary>
        /// <returns></returns>
        public static string GetTableName()
        {            
            Type[] typeArgs = { typeof(T) };            
            string className = typeArgs[0].Name;
            string tableName = string.Empty;
            if (DbMappingTables.ContainsKey(className) == true)
                tableName = DbMappingTables[className].TableName;

            return tableName;
        }
        /// <summary>
        /// 取得欄位主鍵名稱
        /// </summary>
        /// <returns></returns>
        public static string GetPrimaryKeyName()
        {            
            Type[] typeArgs = { typeof(T) };            
            string className = typeArgs[0].Name;
            string primaryKey = string.Empty;
            if (DbMappingTables.ContainsKey(className) == true)
                primaryKey = DbMappingTables[className].PrimaryKey;

            return primaryKey;
        }
        /// <summary>
        /// 取得連外鍵欄位名稱
        /// </summary>
        /// <returns></returns>
        public static string GetRelatedTablePrimaryKey()
        {            
            Type[] typeArgs = { typeof(T) };            
            string className = typeArgs[0].Name;
            string relatedKey = string.Empty;
            if (DbMappingTables.ContainsKey(className) == true)
                relatedKey = DbMappingTables[className].RelatedTablePrimaryKey;

            return relatedKey;
        }
        /// <summary>
        /// 建立物件實例
        /// </summary>
        /// <returns></returns>
        public static object CreateInstance()
        {
            //Type type = typeof(IReadOnlyDictionary<,>);
            var service = typeof(CommonRepositoryService<>);
            Type[] typeArgs = { typeof(T) };
            var makeme = service.MakeGenericType(typeArgs);            
            object obj = Activator.CreateInstance(makeme);
            return obj;
        }
        #endregion
    }
    #endregion
}
