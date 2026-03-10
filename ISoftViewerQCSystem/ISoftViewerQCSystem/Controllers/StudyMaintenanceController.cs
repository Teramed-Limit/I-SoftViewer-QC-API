using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerQCSystem.Applications;
using ISoftViewerQCSystem.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public class StudyMaintenanceController : ControllerBase, IHandleCommand
    {
        /// <summary>
        ///     建構
        /// </summary>
        /// <param name="cmdServices"></param>
        /// <param name="logger"></param>
        public StudyMaintenanceController(
            IEnumerable<IApplicationCmdService> cmdServices,
            ILogger<StudyMaintenanceController> logger,
            DicomImageService dicomImageService,
            DicomImagePathViewService dicomImagePathService)
        {
            Logger = logger;
            _dicomImageService = dicomImageService;
            _dicomImagePathService = dicomImagePathService;
            var applicationCmdServices = cmdServices as IApplicationCmdService[] ?? cmdServices.ToArray();
            DcmStudyMaintenanceService =
                (DcmDataCmdApplicationService)applicationCmdServices.Single(x =>
                    x.CmdServiceType == CmdServiceType.DcmData);
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
                if (User.Identity == null) return BadRequest("User not exist.");
                await handler(User.Identity.Name, command);
                return Ok();
            }
            catch (Exception e)
            {
                // Logger.LogError("Error handling the request", e);
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        ///     Create Study建立檢查含Pixel Data
        /// </summary>
        /// <param name="value"></param>
        [HttpPost]
        public Task<IActionResult> Post(
            [FromBody] DataCorrection.V1.CreateAndModifyStudy<DataCorrection.V1.ImageBufferAndData> value)
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
        public Task<IActionResult> Put(
            [FromBody] DataCorrection.V1.StudyMappingParameter<DataCorrection.V1.DcmTagData> value)
        {
            return HandleCommand(value, StudyQcApplicationService.Handle);
        }

        /// <summary>
        ///     病歷檢查資料與外部資料做資料批配校正
        /// </summary>
        /// <param name="value"></param>
        [Route("mapping/multi")]
        [HttpPut]
        public Task<IActionResult> PutMulti(
            [FromBody] DataCorrection.V1.StudyMappingParameter<List<DataCorrection.V1.DcmTagData>> value)
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
        ///     刪除單張影像 (Instance 層級)
        /// </summary>
        /// <param name="instanceUid">SOP Instance UID</param>
        /// <returns></returns>
        [HttpDelete("images/{instanceUid}")]
        public ActionResult DeleteImage(string instanceUid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(instanceUid))
                    return BadRequest("Instance UID is required.");
                
                // 刪除資料庫記錄
                var deleted = _dicomImageService.Delete(instanceUid);
                if (!deleted)
                    return NotFound($"Image with instance UID '{instanceUid}' not found.");
                
                Logger.LogInformation("Image deleted successfully. InstanceUID: {InstanceUID}, User: {User}",
                    instanceUid, User.Identity?.Name);

                return Ok(new { message = "Image deleted successfully.", instanceUid });
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to delete image. InstanceUID: {InstanceUID}", instanceUid);
                return BadRequest(e.Message);
            }
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
        ///     DICOM 影像資料庫服務
        /// </summary>
        private readonly DicomImageService _dicomImageService;

        /// <summary>
        ///     DICOM 影像路徑查詢服務
        /// </summary>
        private readonly DicomImagePathViewService _dicomImagePathService;

        /// <summary>
        ///     日誌記錄器
        /// </summary>
        private readonly ILogger<StudyMaintenanceController> Logger;

        #endregion
    }
}