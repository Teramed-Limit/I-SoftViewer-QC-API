using System;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerQCSystem.Services;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     組態設定控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DicomOperationNodeService _dicomOperationNodeService;
        private readonly DicomDestinationNodeService _dicomDestinationNodeService;
        private readonly ICommonRepositoryService<SvrDcmNodeDb> _dicomNodeService;

        public ConfigurationController(
            IConfiguration configuration,
            DicomOperationNodeService dicomOperationNodeService,
            DicomDestinationNodeService dicomDestinationNodeService,
            ICommonRepositoryService<SvrDcmNodeDb> dicomNodeService)
        {
            _dicomOperationNodeService = dicomOperationNodeService;
            _dicomDestinationNodeService = dicomDestinationNodeService;
            _dicomNodeService = (DbTableService<SvrDcmNodeDb>)dicomNodeService;
            _configuration = configuration;
        }

        #region DicomOperationNodes

        /// <summary>
        ///     取得DicomNode
        /// </summary>
        [HttpGet("dicomOperationNode")]
        public ActionResult<IEnumerable<DicomOperationNodes>> GetDicomNodeConfig()
        {
            return Ok(_dicomOperationNodeService.GetAll());
        }
        
        /// <summary>
        ///     根據Type取得Enable的DicomNode
        /// </summary>
        [HttpGet("dicomOperationNode/operationType/{type}")]
        public ActionResult GetCFindNode(string type)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "OperationType", Value = type },
                new() { Name = "Enable", Value = "1", Type = FieldType.ftInt },
            };

            return Ok(_dicomOperationNodeService.Get(where));
        }

        /// <summary>
        ///     增加DicomOperationNode
        /// </summary>
        [HttpPost("dicomOperationNode")]
        public ActionResult AddDicomNodeConfig([FromBody] DicomOperationNodes data)
        {
            var userName = User.Identity?.Name;
            if (!_dicomOperationNodeService.AddOrUpdate(data, userName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     更新DicomOperationNode
        /// </summary>
        [HttpPut("dicomOperationNode/{name}")]
        public ActionResult PostDicomNodeConfig([FromBody] DicomOperationNodes data, string name)
        {
            var userName = User.Identity?.Name;
            if (!_dicomOperationNodeService.AddOrUpdate(data, userName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     刪除DicomOperationNode
        /// </summary>
        [HttpDelete("dicomOperationNode/{name}")]
        public ActionResult Delete(string name)
        {
            if (!_dicomOperationNodeService.Delete(name))
                return BadRequest();

            return Ok();
        }

        #endregion


        #region DicomDestinationNodes

        /// <summary>
        ///     取得DicomNode
        /// </summary>
        [HttpGet("dicomDestinationNode")]
        public ActionResult<IEnumerable<DicomDestinationNode>> GetDDcomDestinationNode()
        {
            return Ok(_dicomDestinationNodeService.GetAll());
        }

        /// <summary>
        ///     增加DicomDestinationNodes
        /// </summary>
        [HttpPost("dicomDestinationNode")]
        public ActionResult AddDicomDesNodeConfig([FromBody] DicomDestinationNode data)
        {
            var userName = User.Identity?.Name;
            if (!_dicomDestinationNodeService.AddOrUpdate(data, userName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     更新DicomDestinationNodes
        /// </summary>
        [HttpPut("dicomDestinationNode/{name}")]
        public ActionResult PostDicomDesNodeConfig([FromBody] DicomDestinationNode data, string name)
        {
            var userName = User.Identity?.Name;
            if (!_dicomDestinationNodeService.AddOrUpdate(data, userName)) return BadRequest();
            return Ok();
        }

        /// <summary>
        ///     刪除DicomDestinationNodes
        /// </summary>
        [HttpDelete("dicomDestinationNode/{name}")]
        public ActionResult DeleteDicomDesNodeConfig(string name)
        {
            var nodeListName = CheckRoutingDestination(name);
            if (nodeListName != null)
                return BadRequest($"Routing destination {name} is used by {nodeListName}, please remove it first");
            
            _dicomDestinationNodeService.GenerateNewTransaction();
            if (!_dicomDestinationNodeService.Delete(name))
                return BadRequest("Delete failed");

            return Ok();
        }

        private string CheckRoutingDestination(string name)
        {
            _dicomNodeService.GenerateNewTransaction();
            var nodeList = _dicomNodeService
                .GetAll()
                .Where(x => x.AuotRoutingDestination.Contains(name))
                .ToList();

            if (nodeList.Count == 0)
                return null;

            var nodeName = nodeList.Select(x => x.Name).ToList();
            return string.Join(",", nodeName);
        }

        #endregion


        /// <summary>
        ///     Restart PACS Service
        /// </summary>
        [HttpPost("restart/service")]
        public IActionResult RestartService()
        {
            try
            {
                Serilog.Log.Information("Restart PACS service");

                var serviceMachineName = _configuration["MachineName"];
#pragma warning disable CA1416
                var sc = new ServiceController("TeraMedArchivingService", serviceMachineName);
                if ((sc.Status.Equals(ServiceControllerStatus.Stopped)) ||
                    (sc.Status.Equals(ServiceControllerStatus.StopPending)))
                {
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
                else
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    sc.Start();
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
#pragma warning restore CA1416
                return Ok(new { message = "Restart service success" });
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "Restart service failed");
                return BadRequest($"Restart service failed, {e.Message}, {e.InnerException.Message}");
            }
        }
    }
}