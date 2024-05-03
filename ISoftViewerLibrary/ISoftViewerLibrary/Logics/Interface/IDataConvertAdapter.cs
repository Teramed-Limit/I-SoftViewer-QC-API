using ISoftViewerLibrary.Models.Interfaces;

namespace ISoftViewerLibrary.Logic.Interfaces
{
    #region IDataConvertAdapter<T1,T2>
    /// <summary>
    /// 資料轉換器介面
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public interface IDataConvertAdapter<T1,T2> : IOpMessage
    {
        /// <summary>
        /// 從fromData轉換到指定的物件型態
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        bool Convert(T1 fromData, T2 convertTo);        
    }
    #endregion
}
