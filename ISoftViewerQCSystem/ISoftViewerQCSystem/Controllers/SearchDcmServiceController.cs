using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerQCSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    /// 查詢控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SearchDcmServiceController : ControllerBase, IHandleSearch
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SearchDcmServiceController(IApplicationQueryService applicationQueryService, ILogger<SearchDcmServiceController> logger)
            : base()
        {
            QueryAppService = applicationQueryService;
            Logger = logger;
        }

        #region Fields
        /// <summary>
        /// 應用層查詢服務
        /// </summary>
        private readonly IApplicationQueryService QueryAppService;
        /// <summary>
        /// 日誌記錄器
        /// </summary>
        private readonly ILogger<SearchDcmServiceController> Logger;
        #endregion

        #region Methods
        /// <summary>
        /// 透過MatchKeys來查詢資料(Worklist)
        /// </summary>
        /// <param name="matchKeys"></param>
        /// <returns></returns>
        [Route("worklist")]
        [HttpGet]
        public async Task<ActionResult<Queries.V1.QueryResult>> Worklist([FromQuery] Queries.V1.FindWorklistKeys matchKeys)
        {
            return await HandleSearch(matchKeys, QueryAppService.Handle);
        }
        /// <summary>
        /// 透過MatchKeys來查詢資料(QR-Find)
        /// </summary>
        /// <param name="matchKeys"></param>
        /// <returns></returns>
        [Route("qrfind")]
        [HttpGet]
        public async Task<ActionResult<Queries.V1.QueryResult>> CFind([FromQuery] Queries.V1.FindQRKeys matchKeys)
        {
            return await HandleSearch(matchKeys, QueryAppService.Handle);
        }
        /// <summary>
        /// 透過MatchKeys來查詢資料(QR-Move)
        /// </summary>
        /// <param name="matchKeys"></param>
        /// <returns></returns>
        [Route("qrmove")]
        [HttpGet]
        public async Task<ActionResult<Queries.V1.QueryResult>> CMove([FromQuery] Queries.V1.MoveQRKeys matchKeys)
        {
            return await HandleSearch(matchKeys, QueryAppService.Handle);
        }
        /// <summary>
        /// 查詢服務
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task<ActionResult<Queries.V1.QueryResult>> HandleSearch<T>(T request, Func<string, T, Task<Queries.V1.QueryResult>> handler)
        {            
            try
            {
                if (User.Identity == null) return BadRequest("User not exist.");
                return Ok(await handler(User.Identity.Name, request));
            }
            catch (Exception e)
            {
                Logger.LogError("Error handling the request", e);
                return BadRequest(e.Message);
            }
        }
        #endregion
    }
}
