using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IJsonDataset
    /// <summary>
    /// 前後端JSON資料應用介面
    /// </summary>
    public interface IJsonDataset
    {
        /// <summary>
        /// JSON欄位取出容器
        /// </summary>        
        IDictionary<string, Func<string>> DataRetrievalFuncs { get; }
        /// <summary>
        /// JSON欄位寫入容器
        /// </summary>
        IDictionary<string, Action<string>> DataWritingActions { get; }
    }
    #endregion
}
