using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerLibrary.Utils;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     DB Dicom Query
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DicomDbQueryController : ControllerBase
    {
        private readonly DicomImageService _dicomImageService;
        private readonly DicomSeriesService _dicomSeriesService;
        private readonly DicomStudyService _dicomStudyService;
        private readonly DicomPatientService _dicomPatientService;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomPatientStudyViewService _dicomPatientStudyService;
        private readonly IMapper _mapper;

        public DicomDbQueryController(
            DicomPatientService dicomPatientService,
            DicomStudyService dicomStudyService,
            DicomSeriesService dicomSeriesService,
            DicomImageService dicomImageService,
            DicomImagePathViewService dicomImagePathService,
            DicomPatientStudyViewService dicomPatientStudyService,
            IMapper mapper)
        {
            _dicomPatientService = dicomPatientService;
            _dicomStudyService = dicomStudyService;
            _dicomSeriesService = dicomSeriesService;
            _dicomImageService = dicomImageService;
            _dicomImagePathService = dicomImagePathService;
            _dicomPatientStudyService = dicomPatientStudyService;
            _mapper = mapper;
        }

        /// <summary>
        ///     Patient/Study View data
        /// </summary>
        [HttpGet]
        public ActionResult<SearchPatientStudyView> GetPatientStudy([FromQuery] Queries.V1.QueryDBKeys queryParams)
        {
            try
            {
                TrimObjectHelper.Trim(queryParams);
                return Ok(_dicomPatientStudyService.Query(queryParams));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        /// <summary>
        ///     Patient level data
        /// </summary>
        [HttpGet("patientId/{patientId}")]
        public ActionResult<DicomPatientData> GetPatient(string patientId)
        {
            return Ok(_dicomPatientService.Get(patientId).First());
        }

        /// <summary>
        ///     Study level data
        /// </summary>
        [HttpGet("studyInstanceUID/{studyInstanceUID}")]
        public ActionResult<DicomStudyData> GetStudy(string studyInstanceUID)
        {
            return Ok(_dicomStudyService.Get(studyInstanceUID).First());
        }

        /// <summary>
        ///     Series level data
        /// </summary>
        [HttpGet("studyInstanceUID/{studyInstanceUID}/series")]
        public ActionResult<IEnumerable<DicomSeriesData>> GetSeries(string studyInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
            };
            return Ok(_dicomSeriesService.Get(where));
        }

        /// <summary>
        ///     Image level data
        /// </summary>
        [HttpGet("seriesInstanceUID/{seriesInstanceUID}/images")]
        public ActionResult<IEnumerable<DicomImageData>> GetImage(string seriesInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SeriesInstanceUID", Value = seriesInstanceUID }
            };

            var order = new List<PairDatas>
            {
                new() { Name = "cast(ImageNumber as int)", Value = "", OrderType = OrderOperator.foASC }
            };

            var dicomImageDto = _mapper.Map<IEnumerable<DicomImageDataDto>>(_dicomImageService.Get(where, order));
            return Ok(dicomImageDto);
        }

        /// <summary>
        ///     ImagePath level data by (StudyInstanceUID)
        /// </summary>
        [HttpGet("studyInstanceUID/{studyInstanceUID}/imagePathList")]
        public ActionResult<IEnumerable<SearchImagePathView>> GetImagePathByStudyInstanceUID(string studyInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
            };

            var columns = new List<PairDatas>
            {
                new() { Name = "HttpFilePath" },
                new() { Name = "SOPInstanceUID" },
                new() { Name = "Annotations" },
                new() { Name = "KeyImage", Type = FieldType.ftBoolean },
                // new() { Name = "ImageNumber", OrderType = OrderOperator.foDESC},
            };

            var dicomImagePathDto =
                _mapper.Map<IEnumerable<SearchImagePathViewDto>>(
                    _dicomImagePathService.GetSpecifyColumn(where, columns));
            return Ok(dicomImagePathDto);
        }


        /// <summary>
        ///     ImagePath level data by (SeriesInstanceUID)
        /// </summary>
        [HttpGet("seriesInstanceUID/{seriesInstanceUID}/imagePathList")]
        public ActionResult<IEnumerable<SearchImagePathViewDto>> GetImagePathByStudySeriesInstanceUID(
            string seriesInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SeriesInstanceUID", Value = seriesInstanceUID }
            };

            var columns = new List<PairDatas>
            {
                new() { Name = "HttpFilePath" },
                new() { Name = "SOPInstanceUID" },
                new() { Name = "Annotations" },
                new() { Name = "KeyImage", Type = FieldType.ftBoolean },
                new() { Name = "ImageNumber", Type = FieldType.ftInt, OrderType = OrderOperator.foASC },
            };

            var dicomImagePathDto =
                _mapper.Map<IEnumerable<SearchImagePathViewDto>>(
                    _dicomImagePathService.GetSpecifyColumn(where, columns));
            return Ok(dicomImagePathDto);
        }
    }
}