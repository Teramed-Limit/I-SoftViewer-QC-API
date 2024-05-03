using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerLibrary.Models.DTOs.PacsServer;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsStorageDeviceController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="device"></param>
        public PacsStorageDeviceController(ICommonRepositoryService<SvrFileStorageDevice> device)
        {
            PacsDeviceService = (DbTableService<SvrFileStorageDevice>)device;
        }

        #region Fields        
        /// <summary>
        /// StorageDevice資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrFileStorageDevice> PacsDeviceService;
        #endregion

        #region Methods
        /// <summary>
        /// 取得儲存裝置資料
        /// </summary>
        /// <returns></returns>
        [HttpGet("stdevice")]
        public ActionResult<IEnumerable<SvrFileStorageDevice>> GetStorageDevice()
        {
            List<SvrFileStorageDevice> result = new();
            IEnumerable<SvrFileStorageDevice> devices = PacsDeviceService.GetAll();
            foreach (var device in devices)
            {
                device.StorageLevel = DeviceHelper.DbToWebStorageLevel(device.StorageLevel);
                result.Add(device);
            }
            return Ok(result);            
        }
        /// <summary>
        /// 取得PACS SystemConfiguration Log Level
        /// </summary>
        [HttpGet("stdevice/stlevel")]
        public ActionResult<IEnumerable<string>> GetStorageLevel()
        {
            return Ok(new List<string>() { "Image", "Video" });
        }
        /// <summary>
        /// 更新StorageDevice
        /// </summary>
        [HttpPost("stdevice/name/{name}")]
        public ActionResult PostDicomProviderConfig([FromBody] SvrFileStorageDevice data, string name)
        {
            var userName = User.Identity?.Name;
            data.StorageLevel = DeviceHelper.WebToDbStorageLevel(data.StorageLevel);
            if (PacsDeviceService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        #endregion
    }
}
