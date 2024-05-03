using System;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ServiceProcess;
using ISoftViewerLibrary.Services.RepositoryService.Table;
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
        private readonly DicomOperationNodeService _dicomOperationNodeService;
        private readonly DicomDestinationNodeService _dicomDestinationNodeService;
        private readonly IConfiguration _configuration;

        public ConfigurationController(
            DicomOperationNodeService dicomOperationNodeService,
            IConfiguration configuration, DicomDestinationNodeService dicomDestinationNodeService)
        {
            _dicomOperationNodeService = dicomOperationNodeService;
            _dicomDestinationNodeService = dicomDestinationNodeService;
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
            if (!_dicomDestinationNodeService.Delete(name))
                return BadRequest();

            return Ok();
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