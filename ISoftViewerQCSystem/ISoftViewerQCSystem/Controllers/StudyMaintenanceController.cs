using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerQCSystem.Applications;
using ISoftViewerQCSystem.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     病歷檢查維護控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class StudyMaintenanceController : ControllerBase, IHandleCommand
    {
        /// <summary>
        ///     建構
        /// </summary>
        /// <param name="cmdServices"></param>
        /// <param name="logger"></param>
        public StudyMaintenanceController(IEnumerable<IApplicationCmdService> cmdServices, ILogger<StudyMaintenanceController> logger)
        {
            Logger = logger;
            var applicationCmdServices = cmdServices as IApplicationCmdService[] ?? cmdServices.ToArray();
            DcmStudyMaintenanceService = (DcmDataCmdApplicationService)applicationCmdServices.Single(x => x.CmdServiceType == CmdServiceType.DcmData);
            StudyQcApplicationService = applicationCmdServices.Single(x => x.CmdServiceType == CmdServiceType.StudyQC);
        }

        /// <summary>
        ///     處理即記錄日誌
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        public async Task<IActionResult> HandleCommand<T>(T command, Func<string, T, Task> handler)
        {
            try
            {
                Logger.LogTrace("Handling HTTP command of type {type}", typeof(T).Name);
                if(User.Identity == null) return BadRequest("User not exist.");
                await handler(User.Identity.Name, command);
                return Ok();
            }
            catch (Exception e)
            {
                Logger.LogError("Error handling the request", e);
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        ///     Create Study建立檢查含Pixel Data
        /// </summary>
        /// <param name="value"></param>
        [HttpPost]
        public Task<IActionResult> Post([FromBody] DataCorrection.V1.CreateAndModifyStudy<DataCorrection.V1.ImageBufferAndData> value)
        {
            return HandleCommand(value, DcmStudyMaintenanceService.Handle);
        }

        /// <summary>
        ///     病歷檢查資料校正,合併檢查
        /// </summary>
        /// <param name="value"></param>
        [Route("merge")]
        [HttpPut]
        public Task<IActionResult> Put([FromBody] DataCorrection.V1.MergeStudyParameter value)
        {
            return HandleCommand(value, StudyQcApplicationService.Handle);
        }

        /// <summary>
        ///     病歷檢查資料校正,拆解檢查
        /// </summary>
        /// <param name="value"></param>
        [Route("split")]
        [HttpPut]
        public Task<IActionResult> Put([FromBody] DataCorrection.V1.SplitStudyParameter value)
        {
            return HandleCommand(value, StudyQcApplicationService.Handle);
        }

        /// <summary>
        ///     病歷檢查資料與外部資料做資料批配校正
        /// </summary>
        /// <param name="value"></param>
        [Route("mapping")]
        [HttpPut]
        public Task<IActionResult> Put([FromBody] DataCorrection.V1.StudyMappingParameter value)
        {
            return HandleCommand(value, StudyQcApplicationService.Handle);
        }

        /// <summary>
        ///     病歷檢查資料反批配復原
        /// </summary>
        /// <param name="value"></param>
        [Route("unmapping")]
        [HttpPut]
        public Task<IActionResult> Put([FromBody] DataCorrection.V1.StudyUnmappingParameter value)
        {
            return HandleCommand(value, StudyQcApplicationService.Handle);
        }

        /// <summary>
        ///     刪除檢查
        /// </summary>
        /// <param name="study_id"></param>
        [HttpDelete("{study_id}")]
        public void Delete(string study_id)
        {
        }

        #region Fields

        /// <summary>
        ///     DICOM建立及資料修正
        /// </summary>
        private readonly DcmDataCmdApplicationService DcmStudyMaintenanceService;

        /// <summary>
        ///     DICOM QC
        /// </summary>
        private readonly IApplicationCmdService StudyQcApplicationService;

        /// <summary>
        ///     日誌記錄器
        /// </summary>
        private readonly ILogger<StudyMaintenanceController> Logger;

        #endregion
    }
}