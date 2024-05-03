using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlUpdateCmdExecutor

    /// <summary>
    /// 用來更新記錄到資料庫中
    /// </summary>
    public class SqlUpdateCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlUpdateCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 執行更新記錄
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;
            
            //要把CreateDateTime忽略
            var foundField = element.DBNormalFields.Find(x => x.FieldName == "CreateDateTime");
            if (foundField != null)
                element.DBNormalFields.Remove(foundField);

            //要把CreateUser忽略
            foundField = element.DBNormalFields.Find(x => x.FieldName == "CreateUser");
            if (foundField != null)
                element.DBNormalFields.Remove(foundField);

            //要把ModifiedDateTime時間補上
            foundField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (foundField != null)
                //foundField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                foundField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);
            
            //要把ModifiedUser補上
            foundField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedUser");
            //MOD BY JB 20211025 改為强制資料判斷
            if (foundField != null)
                //foundField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                foundField.UpdateDbFieldValues("Admin", "", null);

            string sql = " UPDATE " + element.TableName + " SET ";
            string updateFields = "";
            //組合要更新資料的欄位
            foreach (var field in element.DBNormalFields)
            {
                //需要在確認Update要略過的欄位
                if (field.UpdateSqlByPass == true)
                    continue;

                if (updateFields.Trim() != "")
                    updateFields += ", ";
                //MOD BY JB 20210122 要避免單引號組合SQL語法造成的問題 MOD BY JB 20210125 加入N前綴字
                updateFields += field.FieldName + " = N'" + field.Value.Replace("'", "''") + "' ";
            }

            //組合Where條件
            string where = "";
            foreach (var field in element.DBPrimaryKeyFields)
            {
                if (where.Trim() != "")
                    where += " And ";

                where += field.FieldName + " = '" + field.Value + "' ";
            }

            //組合完整的sql語法
            sql = sql + updateFields + " Where " + where;
            return SqlExecuteNonQuery(sql);
        }

        #endregion
    }

    #endregion

    #region SqlUpdateBinaryCmdExecutor

    /// <summary>
    /// 更新二進位欄位的修改器
    /// </summary>
    public class SqlUpdateBinaryCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlUpdateBinaryCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction,
            SqlCommandExecutor sqlExecutor)
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods

        /// <summary>
        /// 執行更新記錄
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;

            //要把CreateDateTime忽略
            var foundField = element.DBNormalFields.Find(x => x.FieldName == "CreateDateTime");
            if (foundField != null)
                element.DBNormalFields.Remove(foundField);

            //要把CreateUser忽略
            foundField = element.DBNormalFields.Find(x => x.FieldName == "CreateUser");
            if (foundField != null)
                element.DBNormalFields.Remove(foundField);

            //要把ModifiedDateTime時間補上
            //不一定會有此欄位
            foundField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedDateTime");
            //MOD BY JB 20211025 改為强制資料判斷
            if (foundField != null)
                //foundField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                foundField.UpdateDbFieldValues(DateTime.Now.ToString("yyyyMMddHHmmss"), "", null);
            
            //要把ModifiedUser補上
            foundField = element.DBNormalFields.Find(x => x.FieldName == "ModifiedUser");
            //MOD BY JB 20211025 改為强制資料判斷
            if (foundField != null)
                //foundField.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                foundField.UpdateDbFieldValues("Admin", "", null);

            //ADD 20210522 Oscar 要先參數刪除,避免下次再寫入資料的時候參數已經重複
            SQLCmd.Parameters.Clear();

            string sql = " UPDATE " + element.TableName + " SET ";
            string updateFields = "";
            //組合要更新資料的欄位
            foreach (var field in element.DBNormalFields)
            {
                //需要在確認Update要略過的欄位
                if (field.UpdateSqlByPass == true)
                    continue;
                //MOD BY JB 20210427 改用Update MakeSqlValue組合字串
                updateFields = MakeSqlValue(updateFields, field.FieldName, field.FieldName, "@", "");
                //將內容加入到SQLCommand的Paramters中
                AddParameters(field);
            }

            //組合Where條件
            string where = "";
            foreach (var field in element.DBPrimaryKeyFields)
            {
                where = field.MakeSQL(where);
                //MOD BY JB 20210427 改用Update MakeSqlValue組合字串
                // where = MakeSqlValue(where, field.FieldName, field.FieldName, "@", "", " And ");
                //將條件加入到SQLCommand的Paramters中
                // AddParameters(field);
            }

            //組合完整的sql語法
            sql = sql + updateFields + " Where " + where;
            return SqlExecuteNonQuery(sql);
        }

        /// <summary>
        /// Update的組合欄位與Insert不一樣所以,獨立一個函式來處理 MOD BY JB 20210427
        /// </summary>
        /// <param name="value"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected string MakeSqlValue(string sql, string fieldName, string fieldValue, string frontQuotedString,
            string rearQuotedString, string conjunction = ", ")
        {
            if (sql.Trim() != string.Empty)
                sql += conjunction;
            //MOD BY JB 20210122 要避免單引號組合SQL語法造成的問題
            return sql + fieldName + "=" + frontQuotedString + fieldValue + rearQuotedString;
        }

        #endregion
    }

    #endregion
}