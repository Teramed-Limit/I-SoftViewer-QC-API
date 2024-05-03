using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IBind1stStrategy<T1,T2>
    /// <summary>
    /// 執行一個參數的策略
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public interface IBind1stStrategy<T1, T2> : IOpMessage
    {
        /// <summary>
        /// 執行一個參數的策略
        /// </summary>
        /// <param name="param1"></param>
        /// <returns></returns>
        T1 PerformProcessing(T2 param1);
    }
    #endregion

    #region IBind2ndStrategy<T1,T2,T3>
    /// <summary>
    /// 2個參數的資料產生器
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    public interface IBind2ndStrategy<T1, T2, T3> : IOpMessage
    {
        /// <summary>
        /// 執行處理
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <returns></returns>
        T3 PerformProcessing(T1 param1, T2 param2);
    }
    #endregion

    #region IBind3rdStrategy<T1,T2,T3,T4>
    /// <summary>
    /// 3個參數的資料產生器
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    public interface IBind3rdStrategy<T1, T2, T3, T4> : IOpMessage
    {
        /// <summary>
        /// 執行處理
        /// </summary>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <returns></returns>
        T4 PerformProcessing(T1 param1, T2 param2, T3 param3);
    }
    #endregion
}
