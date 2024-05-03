using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlDeleteCmdExecutor
    /// <summary>
    /// 執行資料庫刪除動作
    /// </summary>
    public class SqlDeleteCmdExecutor : SqlCommandExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlTransaction"></param>
        /// <param name="sqlExecutor"></param>
        public SqlDeleteCmdExecutor(SqlConnection connection, SqlTransaction sqlTransaction, SqlCommandExecutor sqlExecutor) 
            : base(connection, sqlTransaction, sqlExecutor)
        {
        }

        #region Methods
        /// <summary>
        /// 執行刪除資料動作
        /// </summary>
        /// <param name="element"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;

            string sql = "Delete From " + element.TableName;

            //組合Where條件
            void genDelSql(ref string whe, ICommonFieldProperty fld)
            {
                //某些條件下,刪除不一定主鍵全數符合才做刪除動作
                if (fld.FieldName == "" || fld.Value == "" || fld.Value == null)
                    return;
                if (whe.Trim() != "")
                    whe += " And ";

                whe += fld.FieldName + " = '" + fld.Value + "' ";
            }
            string where = "";

            element.DBPrimaryKeyFields.ForEach(field => { genDelSql(ref where, field); });
            element.DBNormalFields.ForEach(field => { genDelSql(ref where, field); });

            //組合完整的sql語法
            sql = sql + " Where " + where;
            return SqlExecuteNonQuery(sql);
        }
        #endregion
    }
    #endregion
}