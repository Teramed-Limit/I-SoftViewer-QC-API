using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IExecutor
    /// <summary>
    /// 執行器,用來做各種類型的操作處理介面,包含資料庫的操作,影像處理等等....
    /// </summary>
    public interface IExecutorInterface : IDisposable
    {
        /// <summary>
        /// 執行
        /// </summary>
        /// <param name="element"></param>
        bool Execute(IElementInterface element, object condition = null);
        /// <summary>
        /// 執行結果
        /// </summary>
        /// <returns></returns>
        int ExecuteResult();
        /// <summary>
        /// 執行過程中是否有任何訊息需要記錄
        /// </summary>
        /// <returns></returns>
        string Messages();
    }
    #endregion
}
