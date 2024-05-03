using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.View;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using ISoftViewerQCSystem.utils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     DB Dicom Query
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ImageRendererController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DicomImagePathViewService _dicomImagePathService;

        public ImageRendererController(
            DicomImagePathViewService dicomImagePathService,
            IConfiguration configuration)
        {
            _dicomImagePathService = dicomImagePathService;
            _configuration = configuration;
        }

        /// <summary>
        ///     Get dcm image
        /// </summary>
        [HttpGet("sopInstanceUID/{sopInstanceUID}/dcm")]
        public ActionResult<string> GetDcmImage(string sopInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SOPInstanceUID", Value = sopInstanceUID }
            };

            var filePath = _dicomImagePathService.Get(where).First().FilePath;
            filePath = _configuration.GetSection("VirtualFilePath").Value + FileUtils.ConvertToWebPath(filePath, ".dcm");

            return Ok(filePath);
        }

        /// <summary>
        ///     Get jpg image
        /// </summary>
        [HttpGet("sopInstanceUID/{sopInstanceUID}/jpg")]
        public ActionResult<IEnumerable<EditableDicomTagData>> GetJpgImage(string sopInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SOPInstanceUID", Value = sopInstanceUID }
            };

            var filePath = _dicomImagePathService.Get(where).First().FilePath;
            filePath = _configuration.GetSection("VirtualFilePath").Value + FileUtils.ConvertToWebPath(filePath, ".jpg");

            return Ok(filePath);
        }
    }
}