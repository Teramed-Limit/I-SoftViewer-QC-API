using System.Collections.Generic;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     記錄日誌控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LogController : ControllerBase
    {
        private readonly OperationRecordService _operationRecordService;
        private readonly QCOperationRecordViewService _qcOperationRecordViewService;

        public LogController(
            OperationRecordService operationRecordService,
            QCOperationRecordViewService qcOperationRecordViewService
        )
        {
            _operationRecordService = operationRecordService;
            _qcOperationRecordViewService = qcOperationRecordViewService;
        }

        /// <summary>
        ///     取得檢查對應的記錄日誌
        /// </summary>
        [HttpGet("qcGuid/{qcGuid}")]
        public IActionResult GetStudyOperationLog(string qcGuid)
        {
            var where = new List<PairDatas> { new() { Name = "QCGuid", Value = qcGuid } };
            return Ok(_operationRecordService.Get(where));
        }

        /// <summary>
        ///     取得使用者經手過的檢查
        /// </summary>
        [HttpGet("userName/{userName}")]
        public IActionResult GetStudyByUserOperateBefore(string userName)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "Operator", Value = userName }
            };
            return Ok(_qcOperationRecordViewService.Get(where));
        }
    }
}