using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region CustomizeTable
    /// <summary>
    /// 客製化表格,不限定那一個資料庫欄位
    /// </summary>
    public class CustomizeTable : ElementAbstract
    {
        /// <summary>
        /// 建構,單純將即有的容器內容清除
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="tableName"></param>
        public CustomizeTable(string userId, string tableName)
            : base(userId)
        {
            TableName = tableName;
        }

        public CustomizeTable(string tableName)
        {
            TableName = tableName;
        }

        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="element"></param>
        public CustomizeTable(IElementInterface element)
            : base(element)
        {
        }
        #region Methods
        /// <summary>
        /// 複製Element,只複製容器的內容
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new CustomizeTable(this);
        }
        #endregion
    }
    #endregion    
}