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
    public class StoreDcmServiceController : ControllerBase
    {
        /// <summary>
        ///     建構
        /// </summary>
        public StoreDcmServiceController(
            DicomImagePathViewService dicomImagePathService,
            DicomOperationNodeService dicomOperationNodeService,
            QCOperationContext qcOperationContext,
            IDcmUnitOfWork netUnitOfWork,
            IDcmRepository dcmRepository)
        {
            _dicomImagePathService = dicomImagePathService;
            _dicomOperationNodeService = dicomOperationNodeService;
            _qcOperationContext = qcOperationContext;
            _netUnitOfWork = netUnitOfWork;
            _dcmRepository = dcmRepository;
        }

        #region Methods

        /// <summary>
        ///     查詢資料庫並C-Store到指定Server AE Title
        /// </summary>
        [HttpPost("studyInstanceUID/{studyInstanceUID}")]
        public async Task<IActionResult> CStoreStudy(string studyInstanceUID, [FromBody] CStoreDetails cStoreDetails)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
            };

            var dicomImageResult = _dicomImagePathService.Get(where);

            // collect study dataset
            foreach (var dicomImage in dicomImageResult)
            {
                var dataset = (await DicomFile.OpenAsync(dicomImage.ImageFullPath)).Dataset;
                _dcmRepository.DicomDatasets.Add(dataset);
            }

            if (cStoreDetails.CreateNewStudy)
            {
                // 列屬於同一個檢查
                // Group by SeriesInstanceUid
                var groupBySeriesInstanceUid =
                    _dcmRepository.DicomDatasets.GroupBy(dataset => dataset.GetString(DicomTag.SeriesInstanceUID));

                // 根據Grouping產生InstanceUID
                var studyInstanceUid = "1.3.6.1.4.1.54514" + "." + DateTime.Now.ToString("yyyyMMddHHmmssffff");
                var seriesIdx = 1;
                foreach (var seriesGroup in groupBySeriesInstanceUid)
                {
                    var seriesInstanceUid = studyInstanceUid + "." + Convert.ToString(seriesIdx);
                    var imageIdx = 1;
                    foreach (var dataset in seriesGroup)
                    {
                        var sopInstanceUid = seriesInstanceUid + "." + Convert.ToString(imageIdx);

                        dataset.AddOrUpdate(DicomTag.StudyInstanceUID, studyInstanceUid);
                        dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, seriesInstanceUid);
                        dataset.AddOrUpdate(DicomTag.SOPInstanceUID, sopInstanceUid);
                        imageIdx++;
                    }

                    seriesIdx++;
                }
            }

            // find c-store node
            var node = _dicomOperationNodeService.GetOperationNode("C-STORE", cStoreDetails.NodeName);
            if (node == null) return BadRequest("Unknown C-Store node name");

            // CStore request
            _netUnitOfWork.RegisterRepository(_dcmRepository);
            _netUnitOfWork.Begin(node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle,
                Types.DcmServiceUserType.dsutStore
            );
            if (await _netUnitOfWork.Commit() == false)
                return BadRequest(_netUnitOfWork.Message);

            _qcOperationContext.SetLogger(new SendToPacsLogger());
            _qcOperationContext.SetParams(User.Identity.Name, studyInstanceUID, "",
                $"Send to PACS server: {node.Name}");
            _qcOperationContext.WriteSuccessRecord();
            return Ok("Store success");
        }

        #endregion

        #region Fields

        /// <summary>
        ///     應用層查詢服務
        /// </summary>
        private readonly DicomImagePathViewService _dicomImagePathService;

        /// <summary>
        ///     應用層查詢服務
        /// </summary>
        private readonly DicomOperationNodeService _dicomOperationNodeService;

        /// <summary>
        ///     DICOM Repository
        /// </summary>
        private readonly IDcmRepository _dcmRepository;

        /// <summary>
        ///     DICOM UnitOfWork
        /// </summary>
        private readonly IDcmUnitOfWork _netUnitOfWork;

        /// <summary>
        ///     使用者QC操作記錄器
        /// </summary>
        private readonly QCOperationContext _qcOperationContext;

        #endregion
    }
}