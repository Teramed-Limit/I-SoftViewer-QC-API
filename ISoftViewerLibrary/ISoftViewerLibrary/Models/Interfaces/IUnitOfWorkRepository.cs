using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IBaseUnitOfWorkRepository
    /// <summary>
    /// 基本的資料儲存庫存取元件
    /// </summary>
    public interface IBaseUnitOfWorkRepository
    {
        /// <summary>
        /// 更新資料庫引擎
        /// </summary>
        IExecutorInterface TransactionContextEngine { set; }
        /// <summary>
        /// 查詢資料庫引擎
        /// </summary>
        IExecutorInterface SelectContextEngine { set; }
    }
    #endregion

    #region IUnitOfWorkRepository
    /// <summary>
    /// 資料存取元件
    /// </summary>
    public interface IUnitOfWorkRepository : IBaseUnitOfWorkRepository
    {
        /// <summary>
        /// 客製化表格的執行器
        /// </summary>
        IExecutorInterface CustomizeTableEexecutor { set; }
        /// <summary>
        /// 刪除表格資料的執行器
        /// </summary>
        IExecutorInterface DeleteTableExecutor { set; }
        /// <summary>
        /// 新增表格資料的執行器
        /// </summary>
        IExecutorInterface InsertTableExecutor { set; }
    }
    #endregion    
}
