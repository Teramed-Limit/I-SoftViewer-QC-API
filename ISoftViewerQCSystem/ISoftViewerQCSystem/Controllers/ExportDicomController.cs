using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerLibrary.Utils;
using Microsoft.AspNetCore.Mvc;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportDicomController : ControllerBase
    {
        private readonly DicomImagePathViewService _dicomImagePathService;

        public ExportDicomController(DicomImagePathViewService dicomImagePathService)
        {
            _dicomImagePathService = dicomImagePathService;
        }

        [HttpGet("dcm/studyInstanceUID/{studyInstanceUID}")]
        public async Task<IActionResult> GetStudyDcmZip(string studyInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
            };

            var dicomImageResult = _dicomImagePathService.Get(where);

            var searchImagePathViews = dicomImageResult as SearchImagePathView[] ?? dicomImageResult.ToArray();
            if(!searchImagePathViews.Any()) return BadRequest("No image in study");

            var dcmImage = searchImagePathViews.First();
            var fileName = $"{dcmImage.PatientId}_{dcmImage.PatientsName}_{dcmImage.StudyDate}";
            var dicomImagePaths = searchImagePathViews.Select(x => x.ImageFullPath);

            var zipArchiver = new ZipArchiver();
            var zipFileMemoryStream = await zipArchiver.Zip(dicomImagePaths);
            return File(zipFileMemoryStream, "application/octet-stream", $"{fileName}.zip");
        }


        [HttpGet("dicomDir/studyInstanceUID/{studyInstanceUID}")]
        public Task<IActionResult> GetStudyDicomDirZip(string studyInstanceUID)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}