using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using ISoftViewerQCSystem.Models;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerLibrary.Models.DTOs.PacsServer;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     組態設定控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PacsServiceNodeController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dicomNodeService"></param>
        /// <param name="routingDestinationNode"></param>
        public PacsServiceNodeController(ICommonRepositoryService<SvrDcmNodeDb> dicomNodeService,            
            ICommonRepositoryService<SvrDcmDestNode> routingDestinationNode)
        {
            DcmNodeService = (DbTableService<SvrDcmNodeDb>)dicomNodeService;
            DcmDestinationNode = (DbTableService<SvrDcmDestNode>)routingDestinationNode;
        }

        #region Fields
        /// <summary>
        /// DicomTable資料庫處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmNodeDb> DcmNodeService;        
        /// <summary>
        /// DicomServiceProvider資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmDestNode> DcmDestinationNode;
        #endregion

        #region Methods
        /// <summary>
        ///     取得DicomNode
        /// </summary>
        [HttpGet("dicomNode")]
        public ActionResult<IEnumerable<SvrDcmNodeWeb>> GetDicomNodeConfig()
        {
            List<SvrDcmNodeWeb> result = new();

            List<SvrDcmNodeDb> dbDatasets = DcmNodeService.GetAll() as List<SvrDcmNodeDb>;
            dbDatasets.ForEach(x => 
            {
                SvrDcmNodeWeb web = new(x);
                result.Add(web);
            });            
            return Ok(result);
        }

        /// <summary>
        /// 取得排程的型態
        /// </summary>
        [HttpGet("dicomNode/scheduldJobType")]
        public ActionResult<IEnumerable<string>> GetScheduleJobTypes()
        {            
            return Ok(NodeHelper.ScheduldJobTypes);
        }

        /// <summary>
        /// 取得繞送DICOM SCP設定
        /// </summary>
        [HttpGet("dicomNode/routingDestination")]
        public ActionResult<IEnumerable<string>> GetRoutingDestination()
        {
            return Ok(DcmDestinationNode.GetAll());
        }

        /// <summary>
        ///     增加DicomNode
        /// </summary>
        [HttpPost("dicomNode")]
        public ActionResult AddDicomNodeConfig([FromBody] SvrDcmNodeWeb data)
        {
            var userName = User.Identity?.Name;

            SvrDcmNodeDb dataset = new(data);
            if (!DcmNodeService.AddOrUpdate(dataset, userName)) 
                return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     更新DicomNode
        /// </summary>
        [HttpPut("dicomNode/{name}")]
        public ActionResult PostDicomNodeConfig([FromBody] SvrDcmNodeWeb data, string name)
        {
            var userName = User.Identity?.Name;
            SvrDcmNodeDb dataset = new(data);
            if (!DcmNodeService.AddOrUpdate(dataset, userName)) 
                return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     刪除DicomNode
        /// </summary>
        [HttpDelete("dicomNode/{name}")]
        public ActionResult Post(string name)
        {
            if (!DcmNodeService.Delete(name))
                return BadRequest();

            return Ok();
        }
        #endregion
    }
}