using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlInsertCmdExecutor

    /// <summary>
    /// 用來新增記錄到資料庫之中
    /// </summary>
    public class SqlInsertCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlInsertCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 執行Insert Into SQL語法
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;

            //插入資料,需要先把CreateDateTime及ModifiedDateTime準備好  
            //歷史資料表格會記錄原始表格的建立日期,所以若有值,則不在繼續處理
            ICommonFieldProperty newDateTimeField = element.DBNormalFields.Find(x => x.FieldName == "CreateDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (newDateTimeField != null && newDateTimeField.Value == "")
                //newDateTimeField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                newDateTimeField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);

            // CreateUser
            ICommonFieldProperty createUserField = element.DBNormalFields.Find(x => x.FieldName == "CreateUser");
            if (createUserField != null && createUserField.Value == "")
                createUserField.UpdateDbFieldValues("Admin", "", null);

            //由於歷史資料表格,會把修改時間拉到主鍵欄位,所以需要額外在做判斷
            ICommonFieldProperty modDateTimeField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (modDateTimeField != null)
                //modDateTimeField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                modDateTimeField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);

            string sql = " INSERT INTO " + element.TableName;
            string fieldNames = "";
            //取得欄位名稱 MOD BY JB 20210125 使用新的MakeSqlValue
            foreach (var field in element.DBPrimaryKeyFields)
            {
                fieldNames = MakeSqlValue(fieldNames, field.FieldName, "", "");
            }

            //取得欄位名稱 MOD BY JB 20210125 使用新的MakeSqlValue
            foreach (var field in element.DBNormalFields)
            {
                if (string.IsNullOrEmpty(field.Value)) continue;
                fieldNames = MakeSqlValue(fieldNames, field.FieldName, "", "");
            }

            //取得欄位資料
            SQLCmd.Parameters.Clear();
            string fieldValues = "";
            foreach (var field in element.DBPrimaryKeyFields)
            {
                // fieldValues = MakeSqlValue(fieldValues, field.Value, "N'", "'");
                //使用新的MakeSqlValue
                fieldValues = MakeSqlValue(fieldValues, field.FieldName, "@", "");
                AddParameters(field);
            }

            foreach (var field in element.DBNormalFields)
            {
                if (string.IsNullOrEmpty(field.Value)) continue;
                // fieldValues = MakeSqlValue(fieldValues, field.Value, "N'", "'");
                //使用新的MakeSqlValue
                fieldValues = MakeSqlValue(fieldValues, field.FieldName, "@", "");
                AddParameters(field);
            }

            sql = sql + "( " + fieldNames + " ) VALUES ( " + fieldValues + " )";

            return SqlExecuteNonQuery(sql);
        }

        #endregion
    }

    #endregion

    #region SqlInsertBinaryCmdExecutor

    /// <summary>
    /// 插入二進位欄位資料的寫入器
    /// </summary>
    public class SqlInsertBinaryCmdExecutor : SqlCommandExecutor
    {
        public SqlInsertBinaryCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 執行Insert Into SQL語法
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;

            //插入資料,需要先把CreateDateTime及ModifiedDateTime準備好  
            //歷史資料表格會記錄原始表格的建立日期,所以若有值,則不在繼續處理
            ICommonFieldProperty newDateTimeField = element.DBNormalFields.Find(x => x.FieldName == "CreateDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (newDateTimeField != null && string.IsNullOrEmpty(newDateTimeField.Value))
                //newDateTimeField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                newDateTimeField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);
            
            // CreateUser
            ICommonFieldProperty createUserField = element.DBNormalFields.Find(x => x.FieldName == "CreateUser");
            if (createUserField != null && createUserField.Value == "")
                createUserField.UpdateDbFieldValues("Admin", "", null);

            //由於歷史資料表格,會把修改時間拉到主鍵欄位,所以需要額外在做判斷
            ICommonFieldProperty modDateTimeField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (modDateTimeField != null)
                //modDateTimeField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                modDateTimeField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);

            string sql = " INSERT INTO " + element.TableName;
            string fieldNames = "";
            //取得欄位名稱 MOD BY JB 20210125 使用新的MakeSqlValue
            foreach (var field in element.DBPrimaryKeyFields)
            {
                fieldNames = MakeSqlValue(fieldNames, field.FieldName, "", "");
            }

            //取得欄位名稱 MOD BY JB 20210125 使用新的MakeSqlValue
            foreach (var field in element.DBNormalFields)
            {
                fieldNames = MakeSqlValue(fieldNames, field.FieldName, "", "");
            }

            //取得欄位資料
            string fieldValues = "";
            foreach (var field in element.DBPrimaryKeyFields)
            {
                //使用新的MakeSqlValue
                fieldValues = MakeSqlValue(fieldValues, field.FieldName, "@", "");
                AddParameters(field);
            }

            foreach (var field in element.DBNormalFields)
            {
                //使用新的MakeSqlValue
                fieldValues = MakeSqlValue(fieldValues, field.FieldName, "@", "");
                AddParameters(field);
            }

            sql = sql + "( " + fieldNames + " ) VALUES ( " + fieldValues + " )";

            return SqlExecuteNonQuery(sql);
        }

        #endregion
    }

    #endregion
}