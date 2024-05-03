using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlGetSchemaCmdExecutor
    /// <summary>
    /// 取得資料表或視圖的資料欄位結構
    /// </summary>
    public class SqlGetSchemaAdvancedCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlExecutor"></param>
        public SqlGetSchemaAdvancedCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction, SqlCommandExecutor sqlExecutor) 
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods
        /// <summary>
        /// 取得指定的客製化表格的資料庫欄位
        /// </summary>
        /// <param name="element"></param>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element, condition) == false || element == null)
                return false;
            //如果已有資料欄位,則不在處理
            if (element.DBPrimaryKeyFields.Count > 0 || element.DBNormalFields.Count > 0 || element.TableName.Trim() == "")
                return true;

            try
            {
                //先確認資料庫連線
                if (SQLConnection.State == ConnectionState.Closed)
                    SQLConnection.Open();

                SQLCmd.Connection = SQLConnection;
                SQLCmd.CommandText = "Select * From " + element.TableName;
                SqlDataReader tableReader = SQLCmd.ExecuteReader(CommandBehavior.KeyInfo);

                IDataRecord record = (IDataRecord)tableReader;

                for (int idx = 0; idx < tableReader.FieldCount; idx++)
                {
                    string fieldName = tableReader.GetName(idx);
                    Type fieldType = tableReader.GetFieldType(idx);                    

                    FieldType ftype;
                    if (fieldType == Type.GetType("System.Int32") || fieldType == Type.GetType("System.UInt32"))
                        ftype = FieldType.ftInt;
                    else if (fieldType == Type.GetType("System.DateTime"))
                        ftype = FieldType.ftDateTime;
                    else if (fieldType == Type.GetType("Byte[]") || fieldType == Type.GetType("System.Byte[]"))
                        ftype = FieldType.ftBinary;
                    else
                        ftype = FieldType.ftString;
                    //MOD BY JB 20211025 改為强制資料判斷
                    //ICommonFieldProperty customizeFields = new TableFieldProperty(fieldName, ftype, false, "", false, true);
                    ICommonFieldProperty customizeFields = new TableFieldProperty()
                                                        .SetDbField(fieldName, ftype, false, false, true, false, FieldOperator.foAnd, OrderOperator.foNone);                                                        
                    element.DBPrimaryKeyFields.Add(customizeFields);
                }
                tableReader.Close();
            }
            catch (System.Exception e)
            {
                DbMessages = e.Message;
                return false;
            }
            return true;
        }
        #endregion
    }
    #endregion

    /// <summary>
    /// 取得資料表或視圖的資料欄位結構(由系統表格取得)
    /// </summary>
    public class SqlGetSchemaAdvanced2CmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlGetSchemaAdvanced2CmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction, SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods
        /// <summary>
        /// 取得指定的客製化表格的資料庫欄位
        /// </summary>
        /// <param name="element"></param>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element, condition) == false || element == null)
                return false;
            //如果已有資料欄位,則不在處理, DBNormalFields容器會有4筆資料
            if (element.DBPrimaryKeyFields.Count > 0 || element.DBNormalFields.Count > 4 || element.TableName.Trim() == "")
                return true;

            try
            {
                //先確認資料庫連線
                if (SQLConnection.State == ConnectionState.Closed)
                    SQLConnection.Open();

                SQLCmd.Connection = SQLConnection;
                //先取得主鍵欄位
                string sqlcmd = " SELECT u.COLUMN_NAME, c.CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS c INNER JOIN ";
                sqlcmd += " INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS u ON c.CONSTRAINT_NAME = u.CONSTRAINT_NAME ";
                sqlcmd += " where u.TABLE_NAME = '" + element.TableName + "' And c.CONSTRAINT_TYPE = 'PRIMARY KEY'";
                SQLCmd.CommandText = sqlcmd;

                List<string> primaryKeys = new List<string>();
                using (SqlDataReader reader = SQLCmd.ExecuteReader())
                {   
                    //有資料,並取得資料
                    if (reader.HasRows == true)
                    {
                        while (reader.Read() == true)
                        {
                            IDataRecord record = (IDataRecord)reader;
                            //填入資料
                            if (record.IsDBNull(record.GetOrdinal("COLUMN_NAME")) != true)
                            {
                                int fieldIdx = record.GetOrdinal("COLUMN_NAME");
                                string nameOfKey = record.GetString(fieldIdx).Trim();
                                primaryKeys.Add(nameOfKey);
                            }
                        }                        
                    }
                }
                //取得所有欄位
                SQLCmd.CommandText = "Select * From INFORMATION_SCHEMA.COLUMNS Where TABLE_NAME = '" + element.TableName + "'";
                using (SqlDataReader reader = SQLCmd.ExecuteReader())
                {
                    //目前只讀取一筆
                    while (reader.Read())
                    {
                        //有資料,並取得資料
                        if (reader.HasRows == true)
                        {
                            IDataRecord record = (IDataRecord)reader;
                            string columnName = "";
                            string dataType = "";
                            string isNullable = "";
                            int fieldIdx = 0;
                            //欄位名稱
                            if (record.IsDBNull(record.GetOrdinal("COLUMN_NAME")) != true)
                            {
                                fieldIdx = record.GetOrdinal("COLUMN_NAME");
                                columnName = record.GetString(fieldIdx).Trim();
                            }
                            //資料型態
                            if (record.IsDBNull(record.GetOrdinal("DATA_TYPE")) != true)
                            {
                                fieldIdx = record.GetOrdinal("DATA_TYPE");
                                dataType = record.GetString(fieldIdx).Trim();
                            }
                            //是否為NULL
                            if (record.IsDBNull(record.GetOrdinal("IS_NULLABLE")) != true)
                            {
                                fieldIdx = record.GetOrdinal("IS_NULLABLE");
                                isNullable = record.GetString(fieldIdx).Trim();
                            }

                            if (columnName == "" || dataType == "" || isNullable == "")
                                continue;
                            
                            FieldType fieldType;
                            if (dataType == "varbinary")
                                fieldType = FieldType.ftBinary;
                            else if (dataType == "int")
                                fieldType = FieldType.ftInt;
                            else
                                fieldType = FieldType.ftString;

                            bool nullable = isNullable != "NO";

                            ICommonFieldProperty newField = null;
                            if (primaryKeys.Contains(columnName))
                            {
                                //主鍵                                
                                //newField = new TableFieldProperty(columnName, fieldType, true, "", nullable, true);
                                newField = new TableFieldProperty()
                                            .SetDbField(columnName, fieldType, true, nullable, true, false, FieldOperator.foAnd, 
                                                OrderOperator.foNone);
                                element.DBPrimaryKeyFields.Add(newField);
                            }
                            else
                            {
                                //非主鍵,若已存在,則不處理
                                if (element.DBNormalFields.Find(x => x.FieldName == "columnName") != null)
                                    continue;
                                //MOD BY JB 20210427 CreateDateTime和CreateUser這二個欄位,Update時,不能更新
                                if (columnName == "CreateDateTime" || columnName == "CreateUser")
                                    //newField = new TableFieldProperty(columnName, fieldType, false, "", nullable, true);
                                    newField = new TableFieldProperty()
                                            .SetDbField(columnName, fieldType, false, nullable, true, false, FieldOperator.foAnd, 
                                                OrderOperator.foNone);
                                else
                                    //newField = new TableFieldProperty(columnName, fieldType, false, "", nullable, false);
                                    newField = new TableFieldProperty()
                                            .SetDbField(columnName, fieldType, false, nullable, false, false, FieldOperator.foAnd, 
                                                OrderOperator.foNone);
                                element.DBNormalFields.Add(newField);
                            }
                        }
                    }                    
                    reader.Close();
                }
                primaryKeys.Clear();
            }
            catch (System.Exception e)
            {
                DbMessages = e.Message;
                return false;
            }
            return true;
        }
        #endregion
    }
}