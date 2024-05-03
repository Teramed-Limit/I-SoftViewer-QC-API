using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     查詢控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EchoDcmServiceController : ControllerBase
    {
        /// <summary>
        ///     建構
        /// </summary>
        public EchoDcmServiceController(IDcmUnitOfWork netUnitOfWork, IDcmRepository dcmRepository)
        {
            _netUnitOfWork = netUnitOfWork;
            _dcmRepository = dcmRepository;
        }

        #region Methods

        /// <summary>
        ///     查詢資料庫並Echo到指定Server AE Title
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CEcho([FromBody] DicomOperationNodes node)
        {
            // CStore request
            _netUnitOfWork.RegisterRepository(_dcmRepository);
            _netUnitOfWork.Begin(node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle,
                Types.DcmServiceUserType.dsutEcho
            );
            if (await _netUnitOfWork.Commit() == false)
                return BadRequest(_netUnitOfWork.Message);

            return Ok("Echo success");
        }

        #endregion

        #region Fields

        /// <summary>
        ///     DICOM Repository
        /// </summary>
        private readonly IDcmRepository _dcmRepository;

        /// <summary>
        ///     DICOM UnitOfWork
        /// </summary>
        private readonly IDcmUnitOfWork _netUnitOfWork;

        #endregion
    }
}