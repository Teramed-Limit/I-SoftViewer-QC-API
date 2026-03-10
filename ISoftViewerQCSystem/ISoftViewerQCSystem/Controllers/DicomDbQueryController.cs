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
using ISoftViewerQCSystem.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     DB Dicom Query
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DicomDbQueryController : ControllerBase
    {
        private readonly DicomImageService _dicomImageService;
        private readonly DicomSeriesService _dicomSeriesService;
        private readonly DicomStudyService _dicomStudyService;
        private readonly DicomPatientService _dicomPatientService;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomPatientStudyViewService _dicomPatientStudyService;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public DicomDbQueryController(
            DicomPatientService dicomPatientService,
            DicomStudyService dicomStudyService,
            DicomSeriesService dicomSeriesService,
            DicomImageService dicomImageService,
            DicomImagePathViewService dicomImagePathService,
            DicomPatientStudyViewService dicomPatientStudyService,
            IMapper mapper,
            IConfiguration configuration)
        {
            _dicomPatientService = dicomPatientService;
            _dicomStudyService = dicomStudyService;
            _dicomSeriesService = dicomSeriesService;
            _dicomImageService = dicomImageService;
            _dicomImagePathService = dicomImagePathService;
            _dicomPatientStudyService = dicomPatientStudyService;
            _mapper = mapper;
            _configuration = configuration;
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
                return Ok(_dicomPatientStudyService.Query(queryParams).OrderBy(x => x.PatientId));
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "GetPatientStudy error: {Message}", e.Message);
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
                new() { Name = "ImageNumber", Value = "" }
            };

            var dicomImageDto = _mapper
                .Map<IEnumerable<DicomImageDataDto>>(_dicomImageService.Get(where, order))
                .OrderBy(x => Convert.ToInt32(x.ImageNumber));
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
                // new() { Name = "Annotations" },
                // new() { Name = "KeyImage", Type = FieldType.ftBoolean },
                new() { Name = "ImageNumber", Type = FieldType.ftInt, OrderType = OrderOperator.foASC },
            };

            var dicomImagePathDto =
                _mapper.Map<IEnumerable<SearchImagePathViewDto>>(
                    _dicomImagePathService.GetSpecifyColumn(where, columns));
            return Ok(dicomImagePathDto);
        }

        /// <summary>
        ///     Preview images for a study (first 6 JPEG thumbnails from the first series)
        /// </summary>
        [HttpGet("studyInstanceUID/{studyInstanceUID}/previewImages")]
        public ActionResult GetPreviewImages(string studyInstanceUID)
        {
            try
            {
                // Get all series for the study
                var seriesWhere = new List<PairDatas>
                {
                    new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
                };
                var seriesList = _dicomSeriesService.Get(seriesWhere)?
                    .OrderBy(s => Convert.ToInt32(string.IsNullOrEmpty(s.SeriesNumber) ? "0" : s.SeriesNumber))
                    .ToList();

                if (seriesList == null || !seriesList.Any())
                    return Ok(Array.Empty<object>());

                var firstSeries = seriesList.First();

                // Get images for the first series
                var imageWhere = new List<PairDatas>
                {
                    new() { Name = "SeriesInstanceUID", Value = firstSeries.SeriesInstanceUID }
                };
                var imageColumns = new List<PairDatas>
                {
                    new() { Name = "HttpFilePath" },
                    new() { Name = "SeriesInstanceUID" },
                    new() { Name = "ImageNumber", Type = FieldType.ftInt, OrderType = OrderOperator.foASC },
                };

                var images = _dicomImagePathService.GetSpecifyColumn(imageWhere, imageColumns)?
                    .OrderBy(x => x.ImageNumber)
                    .Take(6)
                    .ToList();

                if (images == null || !images.Any())
                    return Ok(Array.Empty<object>());

                var virtualFilePath = _configuration.GetSection("VirtualFilePath").Value ?? "";

                var previewImages = images.Select(img => new
                {
                    imageUrl = virtualFilePath + FileUtils.ConvertToWebPath(img.HttpFilePath, ".jpg"),
                    imageNumber = img.ImageNumber
                });

                return Ok(previewImages);
            }
            catch (Exception e)
            {
                Serilog.Log.Error(e, "GetPreviewImages error: {Message}", e.Message);
                return BadRequest(e.Message);
            }
        }
    }
}