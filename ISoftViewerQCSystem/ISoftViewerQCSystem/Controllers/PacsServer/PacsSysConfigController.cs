using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Interfaces;
using Microsoft.Extensions.Logging;
using static ISoftViewerQCSystem.Applications.GeneralApplicationService;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsSysConfigController : ControllerBase, IHandleSearch2
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="pacsConfig"></param>
        /// <param name="service"></param>
        /// <param name="logger"></param>
        public PacsSysConfigController(
            ICommonRepositoryService<SvrConfiguration> pacsConfig,
            PacsSysConfigApplicationService service,
            ICommonRepositoryService<SvrConfigurationsV2> pacsConfigV2,
            ILogger<SearchDcmServiceController> logger)
        {
            PacsConfigDbService = (DbTableService<SvrConfiguration>)pacsConfig;
            PacsConfigDbServiceV2 = (DbTableService<SvrConfigurationsV2>)pacsConfigV2;
            ConfigAppService = service;
            Logger = logger;
        }

        #region Fields

        /// <summary>
        /// SystemConfiguration資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrConfiguration> PacsConfigDbService;

        private readonly DbTableService<SvrConfigurationsV2> PacsConfigDbServiceV2;

        /// <summary>
        ///  SystemConfiguration服務
        /// </summary>
        private readonly PacsSysConfigApplicationService ConfigAppService;

        /// <summary>
        /// 日誌記錄器
        /// </summary>
        private readonly ILogger<SearchDcmServiceController> Logger;

        #endregion

        #region Methods

        /// <summary>
        /// 查詢處理
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="request"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task<ActionResult<T2>> HandleSearch<T1, T2>(T1 request, Func<string, T1, Task<T2>> handler)
        {
            try
            {
                if (User.Identity == null)
                    return BadRequest("User not exist.");
                return Ok(await handler(User.Identity.Name, request));
            }
            catch (Exception e)
            {
                Logger.LogError("Error handling the request", e);
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        /// 取得PACS SystemConfiguration
        /// </summary>
        /// <returns></returns>
        [HttpGet("pacsconfig")]
        public async Task<ActionResult<SvrConfiguration>> GetPacsConfigeData()
        {
            //IEnumerable<SvrConfiguration> tmp = PacsConfigDbService.GetAll();
            //SvrConfiguration result = tmp.FirstOrDefault();
            //result.PACSMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(result.PACSMessageWriteToLog);
            //result.WorklistMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(result.WorklistMessageWriteToLog);
            //result.ScheduleMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(result.ScheduleMessageWriteToLog);

            return await HandleSearch<SvrConfiguration, SvrConfiguration>(null, ConfigAppService.Handle);
        }

        /// <summary>
        /// 取得PACS SystemConfiguration
        /// </summary>
        /// <returns></returns>
        [HttpGet("pacsconfigv2")]
        public IEnumerable<SvrConfigurationsV2> GetPacsConfigDataV2()
        {
            return PacsConfigDbServiceV2.GetAll();
        }

        /// <summary>
        /// 取得PACS SystemConfiguration
        /// </summary>
        /// <returns></returns>
        [HttpPost("pacsconfigv2")]
        public ActionResult UpdatePacsConfigDataV2([FromBody] SvrConfigurationsV2 configuration)
        {
            var result =  PacsConfigDbServiceV2.AddOrUpdate(configuration);
            if (result == false) return BadRequest("Update failed.");
            return Ok();
        }

        /// <summary>
        /// 取得PACS SystemConfiguration Log Level
        /// </summary>
        [HttpGet("pacsconfig/loglevel")]
        public ActionResult<IEnumerable<string>> GetPacsConfigeLogLevel()
        {
            return Ok(ConfigHelper.ServerMessageTypes);
        }

        /// <summary>
        /// 更新SystemConfiguration
        /// </summary>
        [HttpPost("pacsconfig/name/{name}")]
        public ActionResult PostDicomProviderConfig([FromBody] SvrConfiguration data, string name)
        {
            var userName = User.Identity?.Name;
            data.PACSMessageWriteToLog = ConfigHelper.WebLogLevelToDb(data.PACSMessageWriteToLog);
            data.WorklistMessageWriteToLog = ConfigHelper.WebLogLevelToDb(data.WorklistMessageWriteToLog);
            data.ScheduleMessageWriteToLog = ConfigHelper.WebLogLevelToDb(data.ScheduleMessageWriteToLog);

            if (PacsConfigDbService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }

        /// <summary>
        /// 取得I-SoftViewer PACS Service的啟用狀況
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        [HttpPost("pacsconfig/service")]
        public ActionResult<int> PostDicomServiceAction(string action)
        {
            // 0:失敗, 1:啟用成功, 2:開閉成功, 3:Service不存在
            //先判斷TreaMed Window Service是否存在
            bool wndServiceIsExists = false;
            bool wndServiceIsStartup = false;
            const string teraMedArchivingService = "TeraLinkaDicomService";
            ServiceController[] services = ServiceController.GetServices();
            ServiceController teramedWndService = null;
            foreach (ServiceController service in services)
            {
                if (service.ServiceName.ToLower() == teraMedArchivingService.ToLower())
                {
                    wndServiceIsExists = true;
                    wndServiceIsStartup = !(service.Status == ServiceControllerStatus.Stopped);
                    teramedWndService = service;
                    break;
                }
            }

            //不存在,則不處理
            if (wndServiceIsExists == false)
                return BadRequest(3);
            try
            {
                //啟動
                if (action == "Startup")
                {
                    //若已啟動,則不處理
                    if (wndServiceIsStartup == true)
                        return Ok(1);

                    //若未啟動,則啟動(預設等15秒鐘)
                    teramedWndService.Start();
                    for (int i = 0; i < 30; i++)
                    {
                        teramedWndService.Refresh();
                        System.Threading.Thread.Sleep(500);
                        if (teramedWndService.Status == ServiceControllerStatus.Running)
                            return Ok(1);
                    }
                }

                //關閉
                if (action == "Close")
                {
                    //若未啟動,則不處理
                    if (wndServiceIsStartup == false)
                        return Ok("2");
                    //若未啟動,則啟動(預設等15秒鐘)
                    teramedWndService.Stop();
                    for (int i = 0; i < 30; i++)
                    {
                        teramedWndService.Refresh();
                        System.Threading.Thread.Sleep(500);
                        if (teramedWndService.Status == ServiceControllerStatus.Stopped)
                            return Ok("2");
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            //反之,啟動,停止失敗或不支援的參數,則回覆失敗
            return BadRequest(0);
        }

        #endregion
    }
}