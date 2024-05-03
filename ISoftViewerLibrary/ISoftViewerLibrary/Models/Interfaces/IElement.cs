using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IElementInterface
    /// <summary>
    /// 作為後續物件的介面,例如資料庫的資料表等等....
    /// </summary>
    public interface IElementInterface : ICloneable
    {
        #region Fields
        /// <summary>
        /// 資料表格名稱
        /// </summary>
        string TableName { get; }
        /// <summary>
        /// 用來存放非主鍵的欄位
        /// </summary>
        List<ICommonFieldProperty> DBNormalFields { get; }
        /// <summary>
        /// 用來存放主鍵的欄位
        /// </summary>
        List<ICommonFieldProperty> DBPrimaryKeyFields { get; }
        /// <summary>
        /// 從資料庫查詢回來的資料集合
        /// </summary>
        List<List<ICommonFieldProperty>> DBDatasets { get; }
        /// <summary>
        /// 建立此筆資料的使用者帳號
        /// </summary>
        ICommonFieldProperty CreateUser { get; set; }
        /// <summary>
        /// 建立此筆資料的日期時間 MOD BY JB 20200717 protected -> public 
        /// </summary>
        ICommonFieldProperty CreateDateTime { get; set; }
        /// <summary>
        /// 修改此筆資料的使用者帳號 MOD BY JB 20200717 protected -> public 
        /// </summary>
        ICommonFieldProperty ModifiedUser { get; set; }
        /// <summary>
        /// 修改此筆資料的日期時間 MOD BY JB 20200717 protected -> public 
        /// </summary>
        ICommonFieldProperty ModifiedDateTime { get; set; }
        /// <summary>
        /// 當使用查詢器時,要不要從查詢詢器置換資料
        /// </summary>
        bool ReplaceDataFromSelectExecutor { get; set; }
        /// <summary>
        /// 處理過程的訊息
        /// </summary>
        ICollection<string> ElementProcessingMessages { get; }
        /// <summary>
        /// 該欄位是否支援全文檢索查詢        
        /// </summary>
        bool IsSupportFullTextSearch { get; set; }
        /// <summary>
        /// 使用者帳號
        /// </summary>
        string UserId { get; }
        /// <summary>
        /// 記錄是否有查詢到資料
        /// </summary>
        bool HaveDataRow { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// 允許何種類型的操作
        /// </summary>
        /// <param name="executor"></param>
        bool Accept(List<IExecutorInterface> executors);
        /// <summary>
        /// 清除已記錄的資料庫欄位內容及資料
        /// </summary>
        void ClearDBDatasets();        
        /// <summary>
        /// 取得處理訊息
        /// </summary>
        /// <returns></returns>
        ICollection<string> GetMessages();
        /// <summary>
        /// 清除所有欄位資料
        /// </summary>
        void ClearWholeFieldValues();

        /// <summary>
        /// 清除所有欄位
        /// </summary>
        void ClearFields();

        #endregion
    }
    #endregion

    #region IRelatedDetailElement
    /// <summary>
    /// 關聯明細項目
    /// </summary>
    public interface IRelatedDetailElement
    {
        /// <summary>
        /// 關聯細項明細表
        /// </summary>
        List<IElementInterface> DetailElements { get; set; }
    }
    #endregion

    
}
