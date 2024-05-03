using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region FieldType
    /// <summary>
    /// Field的型別
    /// </summary>
    public enum FieldType { ftString = 0x00, ftInt = 0x01, ftDateTime = 0x04, ftBinary = 0x08, ftBoolean = 0x02 };
    /// <summary>
    /// Field用來查詢運算的運算子類型
    /// </summary>
    public enum FieldOperator { foAnd, foOr, forNot, foIn, foLike, foBetween };
    /// <summary>
    /// Field用來排序的運算子類型
    /// </summary>
    public enum OrderOperator { foNone, foDESC, foASC };
    #endregion

    #region ICommonFieldProperty
    /// <summary>
    /// 共用欄位屬位介面
    /// </summary>
    public interface ICommonFieldProperty : ICloneable
    {
        #region Fields
        /// <summary>
        /// 欄位名稱
        /// </summary>
        string FieldName { get; }
        /// <summary>
        /// 欄位型態
        /// </summary>
        FieldType Type { get; }
        /// <summary>
        /// 是否為主鍵
        /// </summary>
        bool IsPrimaryKey { get; }
        /// <summary>
        /// DICOM Tag - Group
        /// </summary>
        ushort DicomGroup { get; }
        /// <summary>
        /// DICOM Tag - Elem
        /// </summary>
        ushort DicomElem { get; }
        /// <summary>
        /// 是否可以為空值
        /// </summary>
        bool IsNull { get; }
        /// <summary>
        /// 資料內容
        /// </summary>
        string Value { get; }
        /// <summary>
        /// 執行Update語法是否要略過此欄位
        /// </summary>
        bool UpdateSqlByPass { get; }
        /// <summary>
        /// 第二組資料內容,通常用來做Between的End資料用
        /// </summary>
        string Value2nd { get; }
        /// <summary>
        /// 二進位資料內容
        /// </summary>
        byte[] BinaryValue { get; }
        /// <summary>
        /// 該欄位是否支援全文檢索查詢        
        /// </summary>
        bool IsSupportFullTextSearch { get; }
        /// <summary>
        /// 用來做資料表查詢的運算子
        /// </summary>
        FieldOperator SqlOperator { get; }
        /// <summary>
        /// 排序
        /// </summary>
        OrderOperator OrderOperator { get; }
        /// <summary>
        /// 欄位別名
        /// </summary>
        string AliasFieldName { get; }
        #endregion

        #region Methods
        /// <summary>
        /// 清除欄位資料
        /// </summary>
        void ResetValue();
        /// <summary>
        /// 自動產生SQL語法
        /// </summary>
        /// <returns></returns>
        string MakeSQL(string @where);
        /// <summary>
        /// 指定資料庫欄位
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="isKey"></param>
        /// <param name="allowNull"></param>
        /// <param name="updateByPass"></param>
        /// <param name="fullTextSearch"></param>
        /// <param name="fieldOperator"></param>
        /// <param name="orderOperator"></param>
        /// <returns></returns>
        ICommonFieldProperty SetDbField(string name, FieldType type, bool isKey, bool allowNull, bool updateByPass, bool fullTextSearch,
            FieldOperator fieldOperator, OrderOperator orderOperator);
        /// <summary>
        /// 指定DICOM欄位資料
        /// </summary>
        /// <param name="group"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        ICommonFieldProperty SetDicomTag(ushort group, ushort elem);
        /// <summary>
        /// 指定資料庫欄位
        /// </summary>
        /// <param name="value1"></param>
        /// <param name="value2"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        ICommonFieldProperty UpdateDbFieldValues(string value1, string value2, byte[] buffer);
        /// <summary>
        /// 指定DICOM資料
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        ICommonFieldProperty UpdateDicomValues(string value);
        /// <summary>
        /// 更新資料及排序方式
        /// </summary>
        /// <param name="value"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        ICommonFieldProperty UpdateDbValueAndOrderBy(string value, OrderOperator order);
        #endregion
    }
    #endregion
}
