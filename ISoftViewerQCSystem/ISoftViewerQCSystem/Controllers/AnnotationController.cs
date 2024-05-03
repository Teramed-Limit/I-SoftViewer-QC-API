using System;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ServiceProcess;
using ISoftViewerLibrary.Models.BodyRequestParams;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Utils;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     組態設定控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AnnotationController : ControllerBase
    {
        private readonly DicomImageService _dicomImageService;

        public AnnotationController(DicomImageService dicomImageService)
        {
            _dicomImageService = dicomImageService;
        }

        /// <summary>
        ///     根據Type取得Enable的DicomNode
        /// </summary>
        [HttpPost("sopInstanceUID/{sopInstanceUID}")]
        public ActionResult SaveAnnotation(string sopInstanceUID, [FromBody] ImageAnnotation annotationJsonStr)
        {
            var tableField = new TableField
            {
                PrimaryFields = new List<PairDatas>
                    { new() { Name = "SOPInstanceUID", Value = sopInstanceUID } },
                NormalFields = new List<PairDatas>
                    { new() { Name = "Annotations", Value = annotationJsonStr.Annotation } },
            };

            _dicomImageService.AddOrUpdate(tableField);
            return Ok();
        }
    }
}