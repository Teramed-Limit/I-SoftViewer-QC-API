using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    /// 功能名稱控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class QCFuncController : ControllerBase
    {
        private readonly QcFunctionService _qcFunctionService;

        public QCFuncController(QcFunctionService qcFunctionService)
        {
            _qcFunctionService = qcFunctionService;
        }
        /// <summary>
        /// 取得功能列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult<IEnumerable<QCFunction>> Get()
        {
            return Ok(_qcFunctionService.GetAll());
        }
    }
}
