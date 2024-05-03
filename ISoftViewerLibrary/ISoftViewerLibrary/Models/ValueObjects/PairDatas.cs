using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.ValueObjects
{
    #region PairDatas
    /// <summary>
    /// 配對資料
    /// </summary>
    public class PairDatas
    {
        /// <summary>
        /// 建構
        /// </summary>
        public PairDatas()
        {
            Name = "";
            Value = "";
            Value2nd = "";
            Type = FieldType.ftString;
            OperatorType = FieldOperator.foAnd;
            OrderType = OrderOperator.foNone;
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public PairDatas(string name, string value, FieldType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }
        #region Fields
        /// <summary>
        /// 資料名稱
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 資料內容
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 資料內容 Between 
        /// </summary>
        public string Value2nd { get; set; }
        /// <summary>
        /// 型態
        /// </summary>
        public FieldType Type { get; set; }
        /// <summary>
        /// 型態
        /// </summary>
        public FieldOperator OperatorType { get; set; }
        /// <summary>
        /// 排序
        /// </summary>
        public OrderOperator OrderType { get; set; }
        #endregion
    }
    #endregion
}