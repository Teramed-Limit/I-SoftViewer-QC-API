using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region CustomizeTableBulder
    /// <summary>
    /// 客製化表格
    /// </summary>
    public class CustomizeTableBuilder : IDisposable
    {
        /// <summary>
        /// 建構
        /// </summary>
        public CustomizeTableBuilder()
        {
            CusTable = null;
        }

        #region Fields
        /// <summary>
        /// 客製化表格
        /// </summary>
        protected CustomizeTable CusTable;
        #endregion

        #region Methods
        /// <summary>
        /// 建立表格式
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public CustomizeTableBuilder InitTable(string userid, string tableName)
        {
            CusTable = userid == null ? new CustomizeTable(tableName) : new CustomizeTable(userid, tableName);
            return this;
        }
        /// <summary>
        /// 建立主鍵欄位資料
        /// </summary>
        /// <param name="condKeys"></param>
        /// <returns></returns>
        public CustomizeTableBuilder CreatePrimaryKeyFields(List<PairDatas> condKeys)
        {
            if (CusTable == null)
                return this;
            condKeys.ForEach(field =>
            {
                //MOD BY JB 20211215 主鍵欄位需要在額外區間查詢
                string value1 = string.Empty;
                string value2 = string.Empty;
                if (field.Value != "")
                {
                    if (field.OperatorType == FieldOperator.foBetween)
                    {
                        var list = field.Value.Split('-');
                        value1 = list[0];
                        if (list.Length >= 2)
                            value2 = list[1];
                    }
                    else if (field.Value.Contains("-"))
                    {
                        //需要在判斷-前後是否為數字,區間,目前只支援日期數字,相容之前沒有代入OperatorType參數
                        value1 = field.Value;
                        var list = field.Value.Split('-');

                        if (list.Length == 2  && 
                            Regex.IsMatch(list[0], @"^[0-9]+$") &&
                            Regex.IsMatch(list[1], @"^[0-9]+$"))
                        {
                            value1 = list[0];
                            value2 = list[1];
                            //要在手動改為Between型態 ADD BY JB 20220118
                            field.OperatorType = FieldOperator.foBetween;
                        }                        
                    }
                    else
                    {
                        if (field.Value != "*")
                            value1 = field.Value;
                    }
                }
                //MOD BY JB 20211025 改為强制資料判斷                
                ICommonFieldProperty fieldProperty = new TableFieldProperty()
                                                        .SetDbField(field.Name, field.Type, true, false, false, false, field.OperatorType, field.OrderType)
                                                        .UpdateDbFieldValues(value1, value2, null);

                int duplicateIndex = CusTable.DBPrimaryKeyFields.FindIndex(property => property.FieldName == fieldProperty.FieldName);
                if (duplicateIndex != -1)
                    CusTable.DBPrimaryKeyFields[duplicateIndex] = fieldProperty;
                else
                    CusTable.DBPrimaryKeyFields.Add(fieldProperty);
            });
            return this;
        }
        /// <summary>
        /// 建立非主鍵欄位資料
        /// </summary>
        /// <param name="condKeys"></param>
        /// <returns></returns>
        public CustomizeTableBuilder CreateNormalKeyFields(List<PairDatas> condKeys)
        {
            if (CusTable == null)
                return this;
            condKeys.ForEach(field =>
            {
                //MOD BY JB 20211025 改為强制資料判斷, 20211118 allowNull : true           
                ICommonFieldProperty fieldProperty = new TableFieldProperty()
                                                        .SetDbField(field.Name, field.Type, false, true, false, false, field.OperatorType, field.OrderType)
                                                        .UpdateDbFieldValues(field.Value, "", null);

                int duplicateIndex = CusTable.DBNormalFields.FindIndex(property => property.FieldName == fieldProperty.FieldName);
                if (duplicateIndex != -1)
                    CusTable.DBNormalFields[duplicateIndex] = fieldProperty;
                else
                    CusTable.DBNormalFields.Add(fieldProperty);
            });
            return this;
        }
        /// <summary>
        /// 回覆CustomeTable
        /// </summary>
        /// <returns></returns>
        public CustomizeTable BuildTable()
        {
            return CusTable;
        }
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

    #region MasterDetailTableBuiler
    /// <summary>
    /// Master/Detail Table Builder
    /// </summary>
    public class MasterDetailTableBuiler : IDisposable
    {
        /// <summary>
        /// 建構
        /// </summary>
        public MasterDetailTableBuiler()
        {
        }

        #region Fields
        /// <summary>
        /// 客製化表格
        /// </summary>
        protected MasterDetailTable CusMasterDetailTable;
        #endregion

        #region Methods
        /// <summary>
        /// 建立表格式
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public MasterDetailTableBuiler InitTable(string userid, string masterTabName, List<string> detailTableNames)
        {
            //建立Master表格
            CusMasterDetailTable = new MasterDetailTable(userid, masterTabName);
            if (detailTableNames == null)
                return this;
            //建立Detail表格
            detailTableNames.ForEach(tbName =>
            {
                //Detail Table先暫時使用CustomizeTable,而沒有使用MasterDetailTable,目前還不需要用到3層關聯資料表
                CustomizeTable detailTable = userid == null ? new CustomizeTable(tbName) : new CustomizeTable(userid, tbName);
                CusMasterDetailTable.DetailElements.Add(detailTable);
            });
            return this;
        }
        /// <summary>
        /// 建立Master/Detail主鍵欄位資料
        /// </summary>
        /// <param name="condKeys"></param>
        /// <returns></returns>
        public MasterDetailTableBuiler SetPrimaryKeyFields(Dictionary<string, List<PairDatas>> condKeys)
        {
            if (CusMasterDetailTable == null)
                return this;

            SetCommonFieldValue(condKeys, true);
            return this;
        }
        /// <summary>
        /// 建立非主鍵欄位資料
        /// </summary>
        /// <param name="condKeys"></param>
        /// <returns></returns>
        public MasterDetailTableBuiler SetNormalKeyFields(Dictionary<string, List<PairDatas>> condKeys)
        {
            if (CusMasterDetailTable == null)
                return this;

            SetCommonFieldValue(condKeys, true);
            return this;
        }
        /// <summary>
        /// 指定欄位資料
        /// </summary>
        /// <param name="condKeys"></param>
        /// <param name="isPrimaryKey"></param>
        protected void SetCommonFieldValue(Dictionary<string, List<PairDatas>> condKeys, bool isPrimaryKey)
        {
            foreach (var condition in condKeys)
            {
                //取得表格名稱
                string tableName = condition.Key;
                //先確認是否為MasterTable,若不是,在確認DetailTable
                IElementInterface tableElement = null;
                if (tableName == CusMasterDetailTable.TableName)
                {
                    tableElement = CusMasterDetailTable;
                }
                else
                {
                    tableElement = CusMasterDetailTable.DetailElements.Find(tab => tab.TableName == tableName);
                }
                //取得主鍵欄位並指定資料
                TableElementHelper tbElementHelper = new();
                condition.Value.ForEach(tmpField =>
                {
                    //有找到欄位,就直接給值,沒有,就建立欄位
                    ICommonFieldProperty fieldProperty = TableElementHelper.FindPrimaryKeyField(tableElement, tmpField.Name);
                    if (fieldProperty != null)
                    {
                        //MOD BY JB 20211025 改為强制資料判斷
                        fieldProperty.UpdateDbValueAndOrderBy(tmpField.Value, tmpField.OrderType);                        
                    }
                    else
                    {
                        //由於Consumable, LabRequests, Medication資料表結構設計問題,所以還需要把非主鍵欄位當成主鍵欄位使用,這裡需要額外處理
                        fieldProperty = TableElementHelper.FindNormalKeyField(tableElement, tmpField.Name);
                        if (fieldProperty != null)
                        {
                            if ((fieldProperty =
                                TableElementHelper.MoveNormalKeyToPrimaryKey(tableElement, tmpField.Name)) != null)
                            {
                                //MOD BY JB 20211025 改為强制資料判斷
                                fieldProperty.UpdateDbValueAndOrderBy(tmpField.Value, tmpField.OrderType);                                
                            }

                        }
                        else
                        {
                            //MOD BY JB 20211025 改為强制資料判斷
                            //fieldProperty = new TableFieldProperty(tmpField.Name, tmpField.Type, true, "", false, true)
                            //{
                            //    Value = tmpField.Value,
                            //    OrderOperator = tmpField.OrderType
                            //};
                            fieldProperty = new TableFieldProperty()
                                                .SetDbField(tmpField.Name, tmpField.Type, true, false, false, false, tmpField.OperatorType, tmpField.OrderType)
                                                .UpdateDbFieldValues(tmpField.Value, "", null);
                            //依照鍵值特性去放入容器
                            if (isPrimaryKey == true)
                                tableElement.DBPrimaryKeyFields.Add(fieldProperty);
                            else
                                tableElement.DBNormalFields.Add(fieldProperty);
                        }
                    }
                });
            }
        }
        /// <summary>
        /// 回覆客製化Master/Detail表格
        /// </summary>
        /// <returns></returns>
        public MasterDetailTable BuildTable()
        {
            return CusMasterDetailTable;
        }
        /// <summary>
        /// 替換MasterDetail
        /// </summary>
        /// <returns></returns>
        public MasterDetailTableBuiler ReplaceTable(MasterDetailTable mdTable)
        {
            CusMasterDetailTable = mdTable;
            return this;
        }
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