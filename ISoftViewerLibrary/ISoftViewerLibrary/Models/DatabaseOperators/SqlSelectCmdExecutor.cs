using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using ISoftViewerLibrary.Models.DTOs;
using Log = Serilog.Log;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlSelectCmdExecutor

    /// <summary>
    /// 用來處理查詢表格的類別
    /// </summary>
    public class SqlSelectCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlExecutor"></param>
        public SqlSelectCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 執行資料庫操作
        /// </summary>
        /// <param name="element"></param>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;

            //組成完成的SQL            
            string sql = MakeSQLScript(element);
            return ExecuteSelectCmd(element, sql);
        }

        /// <summary>
        /// 產生Select語法
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected virtual string MakeSQLScript(IElementInterface element)
        {
            //先取出主鍵的鍵值            
            string where = "";
            string select = "";
            string order = "";

            //先組合Where條件
            foreach (ICommonFieldProperty type in element.DBPrimaryKeyFields)
            {
                if (type.Value.Trim() == "")
                    continue;

                //MOD BY JB 20200413 除了全文檢索之外,其餘的SQL語法皆由欄位自行產生
                if (element.IsSupportFullTextSearch == true && type.IsSupportFullTextSearch == true)
                {
                    List<string> fullTextConditions = type.Value.Trim().Split(' ').ToList();
                    string fulltext = "";
                    fullTextConditions.ForEach(x =>
                    {
                        if (fulltext != string.Empty)
                            fulltext += " And ";
                        fulltext += $" \"*{x}\" ";
                    });
                    fulltext = $"CONTAINS({type.FieldName}, '{fulltext}')";
                    where += fulltext;
                }
                else
                {
                    where = type.MakeSQL(where);
                }
            }

            if (where.Trim() != string.Empty)
                where = " Where " + where;
            //在組合Select條件,主鍵也要包含,不然後續的處理會有問題
            foreach (ICommonFieldProperty field in element.DBPrimaryKeyFields)
            {
                if (select.Trim() != string.Empty)
                    select += ", ";
                select += field.FieldName;

                //Order條件
                if (field.OrderOperator == OrderOperator.foNone)
                    continue;

                string orderType = field.OrderOperator == OrderOperator.foASC ? "ASC" : "DESC";
                if (order.Trim() != string.Empty)
                    select += ", ";
                order += $"{field.FieldName} {orderType}";
            }

            foreach (ICommonFieldProperty field in element.DBNormalFields)
            {
                if (select.Trim() != "")
                    select += ", ";
                select += field.FieldName;

                //Order條件
                if (field.OrderOperator == OrderOperator.foNone)
                    continue;

                string orderType = field.OrderOperator == OrderOperator.foASC ? "ASC" : "DESC";
                if (order.Trim() != string.Empty)
                    order += ", ";
                order += $"{field.FieldName} {orderType}";
            }

            if (order.Trim() != string.Empty)
                order = " Order by " + order;

            //組成完成的SQL  
            if (select.Trim() == "")
                select = " * ";
            string result = " Select " + select + " From " + element.TableName + where + order;
            return result;
        }

        /// <summary>
        /// 執行Select SQL查詢
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public virtual bool ExecuteSelectCmd(IElementInterface element, string sql)
        {
            try
            {
                //執行SQL查詢
                Result = 0;
                if (SQLConnection.State == ConnectionState.Closed)
                    SQLConnection.Open();
                SQLCmd.CommandText = sql;
                using (SqlDataReader reader = SQLCmd.ExecuteReader())
                {
                    //有資料                        
                    if (reader.HasRows)
                    {
                        reader.Close();
                        reader.Dispose();
                        //取得目前有幾筆資料
                        SQLCmd.CommandText = "Select @@ROWCOUNT";
                        Result = (int)SQLCmd.ExecuteScalar();
                    }
                }
                //SQLConnection.Close();
            }
            catch (SystemException e)
            {
                Log.Error(e, "ExecuteSelectCmd Error");
                DbMessages = e.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 依照欄位名稱取得資料
        /// </summary>
        /// <param name="record"></param>
        /// <param name="fe"></param>
        /// <returns></returns>
        protected void GetFieldValue(IDataRecord record, ICommonFieldProperty fe)
        {
            //ADD 20210701 Oscar 要先刪除內容,外面可能有進行複製,如果沒有內容,就會誤用到上一筆record的內容
            if (fe != null)
                fe.ResetValue();

            int fieldIdx = default(int);

            if (fe.FieldName.Trim() == "")
                return;

            //讓查詢取值支援用欄位別名取得欄位資料
            string fieldName = fe.FieldName;
            if (fe.AliasFieldName != "")
                fieldName = fe.AliasFieldName;

            //填入資料
            if (record.IsDBNull(record.GetOrdinal(fieldName)) == true)
                return;

            fieldIdx = record.GetOrdinal(fieldName);
            if (fe.Type == FieldType.ftDateTime)
            {
                //MOD BY JB 20211025 改為强制資料判斷
                fe.UpdateDbFieldValues(
                    record.IsDBNull(fieldIdx) ? "" : record.GetDateTime(fieldIdx).ToString("yyyyMMdd"), "", null);
                //fe.Value = record.IsDBNull(fieldIdx) ? "" : record.GetDateTime(fieldIdx).ToString("yyyyMMdd");
            }
            else if (fe.Type == FieldType.ftInt)
            {
                //MOD BY JB 20211025 改為强制資料判斷
                fe.UpdateDbFieldValues(record.IsDBNull(fieldIdx) ? "" : Convert.ToString(record.GetInt32(fieldIdx)), "",
                    null);
                //fe.Value = record.IsDBNull(fieldIdx) ? "" : Convert.ToString(record.GetInt32(fieldIdx));
            }
            else if (fe.Type == FieldType.ftBinary) //ADD BY JB 20210326 要支援binary欄位
            {
                //先取得資料長度
                long size = record.GetBytes(fieldIdx, 0, null, 0, 0);
                //MOD BY JB 20211025 改為强制資料判斷
                byte[] binaryData = new byte[size];
                ;
                //fe.BinaryValue = new byte[size];
                int bufferSize = 1024;
                long bytesRead = 0;
                int curPos = 0;
                while (bytesRead < size)
                {
                    bytesRead += record.GetBytes(fieldIdx, curPos, binaryData, curPos, bufferSize);
                    curPos += bufferSize;
                }

                fe.UpdateDbFieldValues("", "", binaryData);
            }
            else
            {
                //MOD BY JB 20211025 改為强制資料判斷
                fe.UpdateDbFieldValues(record.IsDBNull(fieldIdx) ? "" : record.GetString(fieldIdx).Trim(), "", null);
                //fe.Value = record.IsDBNull(fieldIdx) ? "" : record.GetString(fieldIdx).Trim();
            }
            //field.Value = (record[field.FieldName] as string).Trim();
            //return (T)Convert.ChangeType(result, typeof(T));
        }

        #endregion
    }

    #endregion

    #region SqlSelectAndFillDataCmdExecutor

    /// <summary>
    /// 只查詢及取得單筆資枓
    /// </summary>
    public class SqlSelectAndFillDataCmdExecutor : SqlSelectCmdExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlExecutor"></param>
        public SqlSelectAndFillDataCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        /// <summary>
        /// 執行Select查詢並將資料代回Entity中
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override bool ExecuteSelectCmd(IElementInterface element, string sql)
        {
            try
            {
                //執行SQL查詢
                if (SQLConnection.State == ConnectionState.Closed)
                    SQLConnection.Open();
                SQLCmd.CommandText = sql;
                using (SqlDataReader reader = SQLCmd.ExecuteReader())
                {
                    //目前只讀取一筆
                    reader.Read();
                    //有資料,並取得資料
                    if (reader.HasRows == true)
                    {
                        IDataRecord record = (IDataRecord)reader;
                        //取出資料並放入主鍵欄位內容之中 MOD BY JB 20210329 由於要處理Binary資料,所以判斷直接移到GetFieldValue函式裡
                        element.DBPrimaryKeyFields.ForEach(field => { GetFieldValue(record, field); });
                        //取出資料並放入一般欄位內容之中 MOD BY JB 20210329 由於要處理Binary資料,所以判斷直接移到GetFieldValue函式裡
                        element.DBNormalFields.ForEach(field => { GetFieldValue(record, field); });
                    }

                    reader.Close();
                }
            }
            catch (SystemException e)
            {
                Log.Error(e, "ExecuteSelectCmd Error");
                DbMessages = e.Message;
                return false;
            }

            return true;
        }
    }

    #endregion

    #region SqlSelectMultiDataCmdExecutor

    /// <summary>
    /// 執行Select查詢,取回多筆資料
    /// </summary>
    public class SqlSelectMultiDataCmdExecutor : SqlSelectCmdExecutor
    {
        public SqlSelectMultiDataCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 將查詢到資料存放Table Class的容器中
        /// </summary>
        /// <param name="element"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override bool ExecuteSelectCmd(IElementInterface element, string sql)
        {
            try
            {
                //先清除舊有的資料
                element.ClearDBDatasets();
                //執行SQL查詢
                if (SQLConnection.State == ConnectionState.Closed)
                    SQLConnection.Open();
                SQLCmd.CommandText = sql;
                using (SqlDataReader reader = SQLCmd.ExecuteReader())
                {
                    //有資料,並取得資料
                    if ((element.HaveDataRow = reader.HasRows) == true)
                    {
                        //讀取已查詢的資料
                        while (reader.Read())
                        {
                            IDataRecord record = (IDataRecord)reader;
                            //Worklist Table不需要主鍵欄位資料,主鍵欄位用來做Where條件用的                            
                            //取出資料並放入一般欄位內容之中  
                            List<ICommonFieldProperty> dataset = new List<ICommonFieldProperty>();

                            void addAndNewDataInField(ICommonFieldProperty tableField)
                            {
                                try
                                {
                                    //MOD BY JB 20210329 由於要處理Binary資料,所以判斷直接移到GetFieldValue函式裡
                                    GetFieldValue(record, tableField);
                                    //先複製原本的資料,在將查詢到的資料放入到新的TableFieldProperty之中
                                    ICommonFieldProperty newFieldData = tableField.Clone() as ICommonFieldProperty;
                                    dataset.Add(newFieldData);
                                }
                                catch (Exception e)
                                {
                                    Serilog.Log.Error(e, "Error field {FieldName} in {TableName}", tableField.FieldName,
                                        element.TableName);
                                    string msg = e.Message;
                                }
                            }

                            element.DBPrimaryKeyFields.ForEach(addAndNewDataInField);
                            element.DBNormalFields.ForEach(addAndNewDataInField);

                            element.DBDatasets.Add(dataset);
                        }
                    }

                    reader.Close();
                }
                //MOD BY 20191220 不在把資料庫連線關閉
                //SQLConnection.Close();
            }
            catch (SystemException e)
            {
                Log.Error(e, "ExecuteSelectCmd Error");
                DbMessages = e.Message;
                return false;
            }

            return true;
        }

        #endregion
    }

    #endregion

    #region SqlWlmSelectCmdExecutor

    /// <summary>
    /// 專門提供給Worklist查詢資料的查詢器
    /// </summary>
    public class SqlWlmSelectCmdExecutor : SqlSelectMultiDataCmdExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlWlmSelectCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 產生Select SQL語法
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected override string MakeSQLScript(IElementInterface element)
        {
            //先取出主鍵的鍵值            
            string where = "";
            string select = "";
            //先組合Where條件
            foreach (ICommonFieldProperty type in element.DBPrimaryKeyFields)
            {
                //Worklist的Match Key比較特別,有可能在Client端沒有填資料
                if (type.FieldName.Trim() == "" || type.Value.Trim() == "" || type.Value.Trim() == "*=*" ||
                    type.Value.Trim() == "*=*=*" ||
                    type.Value.Trim() == "*")
                {
                    continue;
                }

                if (where.Trim() != string.Empty)
                    where = where + " And ";
                //處理 List of UID Matching
                if (type.Value.Contains(@"\") == true)
                {
                    string[] words = type.Value.Split('\\');
                    string inSql = "";
                    foreach (var word in words)
                    {
                        if (inSql.Trim() != "")
                            inSql += " ,";
                        inSql += " '" + word + "' ";
                    }

                    where += type.FieldName + " IN ( " + inSql + " ) ";
                }
                else if (type.Value.Contains("-") == true)
                {
                    //Range Matching
                    string[] words = type.Value.Split('-');
                    where += type.FieldName + " BETWEEN '" + words[0] + "' AND '" + words[1] + "' ";
                }
                else if (type.Value.Contains("*") == true)
                {
                    // Wild Card Mathcing
                    string newValue = type.Value.Replace('*', '%');
                    where += type.FieldName + " LIKE '" + newValue + "' ";
                }
                else if (type.Value.Contains("?") == true)
                {
                    // Wild Card Mathcing
                    where += type.FieldName + " LIKE '" + type.Value + "' ";
                }
                else
                {
                    // Single Value Matching //MOD BY JB 20210122 處理單引號問題
                    where += type.FieldName + " = '" + type.Value.Replace("'", "''") + "' ";
                }
            }

            if (where.Trim() != string.Empty)
                where = " Where " + where;
            //組合Select Script            
            foreach (ICommonFieldProperty field in element.DBNormalFields)
            {
                //有的XML內容並不會有FieldName資料
                if (select.Trim() != "" && field.FieldName.Trim() != "")
                    select += ", ";
                select = select + field.FieldName;
            }

            //組合Order By
            string orderBy = "";
            foreach (ICommonFieldProperty type in element.DBPrimaryKeyFields)
            {
                if (orderBy.Trim() != "")
                    orderBy += ", ";
                orderBy = orderBy + type.FieldName;
            }

            //組成完成的SQL  
            string result = " Select " + select + " From " + element.TableName + where + " Order by " + orderBy;
            return result;
        }

        #endregion
    }

    #endregion
}