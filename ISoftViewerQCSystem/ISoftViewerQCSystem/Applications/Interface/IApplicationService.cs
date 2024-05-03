using ISoftViewerLibrary.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Applications.Interface
{
    #region IApplicationService
    /// <summary>
    /// 應用層查詢服務介面
    /// </summary>
    public interface IApplicationQueryService
    {
        /// <summary>
        /// 處理API動作
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<Queries.V1.QueryResult> Handle(string userName, object command);
    }
    #endregion

    #region IApplicationQueryService<T>
    /// <summary>
    /// 泛形應用層環境服務介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IApplicationQueryService<T>
    {
        /// <summary>
        /// 處理API動作
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<T> Handle(string userName, object command);
    }
    #endregion

    #region IApplicationQueryEnumerateService<T>
    /// <summary>
    /// 泛形應用層環境查詢多組資料介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IApplicationQueryEnumerateService<T>
    {
        /// <summary>
        /// 處理API動作
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<List<T>> HandleMultiple(string userName, object command);
        /// <summary>
        /// 取得特定資料
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<List<string>> HandleMultiple(string userName);
    }
    #endregion

    #region IApplicationCmdService
    /// <summary>
    /// 應用層更新服務介面
    /// </summary>
    public interface IApplicationCmdService
    {
        /// <summary>
        /// 處理API動作
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<Queries.V1.CommandResult> Handle(string userName, object command);
        /// <summary>
        /// 應用層更新服務介面
        /// </summary>
        CmdServiceType CmdServiceType { get; }
    }
    #endregion

    #region IApplicationCmdService<T>
    /// <summary>
    /// 泛形應用層更新服務介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IApplicationCmdService<T>
    {
        /// <summary>
        /// 處理API動作
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        Task<T> Handle(string userName, object command);
    }
    #endregion

    public enum CmdServiceType
    {
        DcmData,
        StudyQC,
    }

}
