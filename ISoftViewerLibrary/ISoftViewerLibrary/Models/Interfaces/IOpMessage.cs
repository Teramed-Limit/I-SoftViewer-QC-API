using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    /// <summary>
    /// 處理列舉值
    /// </summary>
    public enum OpResult { OpSuccess = 0, OpFailure = 1, OpSendFail = 2, OpUpdateFail = 3 };

    #region IOpMessage
    /// <summary>
    /// 處理訊息
    /// </summary>
    public interface IOpMessage
    {
        /// <summary>
        /// 訊息
        /// </summary>
        string Message { get; }
        /// <summary>
        /// 結果
        /// </summary>
        OpResult Result { get; }
    }
    #endregion
}
