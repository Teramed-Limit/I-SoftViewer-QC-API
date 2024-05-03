using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsDestinationNodesController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="routingDestinationNode"></param>
        public PacsDestinationNodesController(ICommonRepositoryService<SvrDcmDestNode> routingDestinationNode)
        {
            DcmDestinationNodeService = (DbTableService<SvrDcmDestNode>)routingDestinationNode;
        }

        #region Fields        
        /// <summary>
        /// DicomServiceProvider資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmDestNode> DcmDestinationNodeService;
        #endregion

        #region Methods
        /// <summary>
        /// 取得Dicom routing node
        /// </summary>
        [HttpGet("routingNode")]
        public ActionResult<IEnumerable<SvrDcmDestNode>> GetDcmDestinationNode()
        {            
            return Ok(DcmDestinationNodeService.GetAll());
        }
        /// <summary>
        /// 取得Dicom routing node
        /// </summary>
        [HttpGet("routingNode/aetitle")]
        public ActionResult<IEnumerable<string>> GetDcmDestinationNodeAETitle()
        {
            List<string> result = new();
            foreach(var routingScp in DcmDestinationNodeService.GetAll())
            {
                if (result.Contains(routingScp.AETitle) == false)
                    result.Add(routingScp.AETitle);
            }
            return Ok(result);
        }
        /// <summary>
        /// 增加Routing Node
        /// </summary>
        [HttpPost("routingNode/logicalName")]
        public ActionResult AddDicomNodeConfig([FromBody] SvrDcmDestNode data)
        {
            var userName = User.Identity?.Name;
            
            if (!DcmDestinationNodeService.AddOrUpdate(data, userName))
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 刪除Routing Node
        /// </summary>
        [HttpDelete("routingNode/logicalName/{logicalName}")]
        public ActionResult Post(string logicalName)
        {
            if (!DcmDestinationNodeService.Delete(logicalName))
                return BadRequest();

            return Ok();
        }
        #endregion
    }
}
