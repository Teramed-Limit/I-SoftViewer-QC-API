using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region MasterDetailTable
    /// <summary>
    /// Maste/Detail表格物件
    /// </summary>
    public class MasterDetailTable : ElementAbstract, IRelatedDetailElement
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="tableName"></param>
        public MasterDetailTable(string userid, string tableName)
            : base(userid)
        {
            TableName = tableName;
            DetailElements = new List<IElementInterface>();
        }

        #region Fields
        /// <summary>
        /// DICOM Tag明細資料表
        /// </summary>
        public List<IElementInterface> DetailElements { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// 執行所有執行器
        /// </summary>
        /// <param name="executors"></param>
        public override bool Accept(List<IExecutorInterface> executors)
        {
            if (base.Accept(executors) == false)
                return false;

            //每一筆Detail資料都要處理到
            bool resultOfExecute = true;
            foreach (var table in DetailElements)
            {
                if ((resultOfExecute = table.Accept(executors)) == false)
                    break;
            }

            return resultOfExecute;
        }
        /// <summary>
        /// 清除已記錄的資料庫欄位內容及資料
        /// </summary>
        public override void ClearDBDatasets()
        {
            foreach (var item in DBDatasets)
            {
                item.Clear();
            }
            //連Detail表格也要做清除
            DetailElements.ForEach(table =>
            {
                table.DBDatasets.ForEach(dataset =>
                {
                    dataset.Clear();
                });
            });
        }
        /// <summary>
        /// 取得Element處理訊息
        /// </summary>
        /// <returns></returns>
        public override ICollection<string> GetMessages()
        {
            DetailElements.ForEach(element => 
            { 
                foreach (var messages in element.ElementProcessingMessages)
                {
                    ElementProcessingMessages.Add(messages);
                }
            });
            return ElementProcessingMessages;
        }
        #endregion
    }
    #endregion
}