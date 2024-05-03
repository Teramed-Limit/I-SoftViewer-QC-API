using ISoftViewerLibrary.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Interfaces
{
    #region IHandleRequest
    /// <summary>
    /// Controller請求服務
    /// </summary>
    public interface IHandleCommand
    {
        /// <summary>
        /// 請求新增,修改及刪除命令
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task<IActionResult> HandleCommand<T>(T command, Func<string, T, Task> handler);
    }
    #endregion

    #region IHandleRequest
    /// <summary>
    /// 請求查詢服務
    /// </summary>
    public interface IHandleSearch
    {
        /// <summary>
        /// 請求服務處理
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task<ActionResult<Queries.V1.QueryResult>> HandleSearch<T>(T request, Func<string, T, Task<Queries.V1.QueryResult>> handler);
    }
    #endregion

    #region IHandleSearch2
    /// <summary>
    /// 泛形請求查詢服務
    /// </summary>
    public interface IHandleSearch2
    {
        /// <summary>
        /// 請求服務處理
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="request"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        Task<ActionResult<T2>> HandleSearch<T1,T2>(T1 request, Func<string, T1, Task<T2>> handler);
    }
    #endregion
}
