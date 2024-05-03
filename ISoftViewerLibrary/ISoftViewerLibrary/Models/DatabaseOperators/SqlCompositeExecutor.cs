using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace ISoftViewerLibrary.Models.DatabaseOperator
{
    #region SqlCompositeExecutor
    /// <summary>
    /// 組合Select, Insert, Update 的SQL執行器
    /// </summary>
    public class SqlCompositeExecutor : SqlSelectCmdExecutor
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="sqlExecutor"></param>
        public SqlCompositeExecutor(SqlConnection connection, SqlTransaction sqlTransaction, SqlCommandExecutor sqlExecutor, bool supportBinaryExecutor) 
            : base(connection, sqlTransaction, sqlExecutor)
        {
            SQLCommandExecutors = new Dictionary<bool, IExecutorInterface>();
            
            SqlCommandExecutor insertExecutor = null;
            SqlCommandExecutor updateExecutor = null;
            //需要額外判斷是否支援二進位的欄位執行器
            if (supportBinaryExecutor == true)
            {
                insertExecutor = new SqlInsertBinaryCmdExecutor(connection, sqlTransaction, sqlExecutor);
                updateExecutor = new SqlUpdateBinaryCmdExecutor(connection, sqlTransaction, sqlExecutor);
            }
            else
            {
                insertExecutor = new SqlInsertCmdExecutor(connection, sqlTransaction, sqlExecutor);
                updateExecutor = new SqlUpdateCmdExecutor(connection, sqlTransaction, sqlExecutor);
            }

            SQLCommandExecutors.Add(false, insertExecutor);
            SQLCommandExecutors.Add(true, updateExecutor);
        }

        #region Fields
        /// <summary>
        /// 執行器容器
        /// </summary>
        protected IDictionary<bool, IExecutorInterface> SQLCommandExecutors;
        #endregion

        #region Methods
        /// <summary>
        /// 執行器
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public override bool Execute(IElementInterface element, object condition = null)
        {
            if (base.Execute(element) == false)
                return false;
            //判斷是否該筆記錄已經存在資料庫
            bool param = (bool)Convert.ChangeType(ExecuteResult(), typeof(bool));

            if (!(SQLCommandExecutors.Where(x => { return x.Key == param; }).First().Value is SqlCommandExecutor executor))
                return false;

            return executor.Execute(element);
        }
        /// <summary>
        /// 可以重覆加入
        /// </summary>
        /// <param name="executor"></param>
        /// <param name="param"></param>
        public void Add(IExecutorInterface executor, bool param)
        {
            if (SQLCommandExecutors.ContainsKey(param))
                SQLCommandExecutors.Remove(param);

            SQLCommandExecutors.Add(param, executor);
        }
        #endregion
    }
    #endregion
}