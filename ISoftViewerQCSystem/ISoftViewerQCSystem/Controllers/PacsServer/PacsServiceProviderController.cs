using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Controllers
{
    #region DcmProviderConfigurationController
    /// <summary>
    /// DicomServiceProvider控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PacsServiceProviderController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="service"></param>
        public PacsServiceProviderController(ICommonRepositoryService<SvrDcmProviderDb> service)
        {
            DcmServiceProviderConfig = (DbTableService<SvrDcmProviderDb>)service;
        }

        #region Fields
        /// <summary>
        /// DicomServiceProvider資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmProviderDb> DcmServiceProviderConfig;
        #endregion

        #region Methods
        /// <summary>
        /// 取得DicomServiceProvider資料表所有資料
        /// </summary>
        [HttpGet("dicomProvider")]
        public ActionResult<IEnumerable<SvrDcmProviderWeb>> GetDicomProviderConfig()
        {
            List<SvrDcmProviderWeb> result = new();

            var dcmScps = DcmServiceProviderConfig.GetAll();
            foreach (var scp in dcmScps)
            {
                SvrDcmProviderWeb webDto = new(scp);
                result.Add(webDto);
            }
            return Ok(result);
        }
        /// <summary>
        /// 取得Provider所有AETitle
        /// </summary>
        /// <returns></returns>
        [HttpGet("dicomProvider/aetitle")]
        public ActionResult<IEnumerable<string>> GetDicomProviderAETitle()
        {
            List<string> result = new();
            var dcmScps = DcmServiceProviderConfig.GetAll();
            foreach (var scp in dcmScps)
            {
                if (result.Contains(scp.AETitle) == false)
                    result.Add(scp.AETitle);
            }
            return Ok(result);
        }
        /// <summary>
        /// 增加DicomServiceProvider        
        /// </summary>
        [HttpPost("dicomProvider")]
        public ActionResult AddDicomProviderConfig([FromBody] SvrDcmProviderWeb data)
        {
            var userName = User.Identity?.Name;

            SvrDcmProviderDb providerDbDto = new(data);
            if (DcmServiceProviderConfig.AddOrUpdate(providerDbDto, userName) == false) 
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 更新DicomServiceProvider
        /// </summary>
        [HttpPut("dicomProvider/{name}")]
        public ActionResult PostDicomProviderConfig([FromBody] SvrDcmProviderWeb data)
        {
            var userName = User.Identity?.Name;
            SvrDcmProviderDb providerDbDto = new(data);
            if (DcmServiceProviderConfig.AddOrUpdate(providerDbDto, userName) == false) 
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 刪除DicomServiceProvider
        /// </summary>
        [HttpDelete("dicomProvider/{name}")]
        public ActionResult Post(string name)
        {
            if (DcmServiceProviderConfig.Delete(name) == false)
                return BadRequest();

            return Ok();
        }
        #endregion
    }
    #endregion
}
