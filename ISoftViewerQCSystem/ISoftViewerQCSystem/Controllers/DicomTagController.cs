using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerQCSystem.Services;
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
    public class DicomTagController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomOperationNodeService _dicomOperationNodeService;
        private readonly DicomTagService _dicomTagService;

        public DicomTagController(
            IConfiguration configuration,
            DicomImagePathViewService dicomImagePathService,
            DicomOperationNodeService dicomOperationNodeService,
            DicomTagService dicomTagService)
        {
            _dicomImagePathService = dicomImagePathService;
            _dicomOperationNodeService = dicomOperationNodeService;
            _dicomTagService = dicomTagService;
            _configuration = configuration;
        }

        /// <summary>
        ///     Get image tag
        /// </summary>
        [HttpGet("sopInstanceUID/{sopInstanceUID}")]
        public ActionResult<IEnumerable<EditableDicomTagData>> GetImageTag(string sopInstanceUID)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "SOPInstanceUID", Value = sopInstanceUID }
            };

            var dcmFileList = _dicomImagePathService.Get(where).ToList();
            if(!dcmFileList.Any()) return BadRequest("Dcm file not found");

            var dcmTags = _dicomTagService.GetDcmTag(
                dcmFileList.Select(x => x.ImageFullPath).First(),
                _configuration.GetSection("ModifiableTag").Get<List<string>>());
            if(dcmTags == null) return BadRequest("Dcm file missing");

            return Ok(dcmTags);
        }

        /// <summary>
        ///     Modify image tag where in studyInstanceUID
        /// </summary>
        [HttpPost("studyInstanceUID/{studyInstanceUID}")]
        public async Task<ActionResult> ModifyTagInStudyInstanceUID(
            [FromBody] ModifyDicomTagData modifyTag, string studyInstanceUID)
        {
            return await ModifyDicomTag(modifyTag, "StudyInstanceUID", studyInstanceUID);
        }

        /// <summary>
        ///     Modify image tag where in SeriesInstanceUID
        /// </summary>
        [HttpPost("seriesInstanceUID/{seriesInstanceUID}")]
        public async Task<ActionResult> ModifyTagInSeriesInstanceUID(
            [FromBody] ModifyDicomTagData modifyTag, string seriesInstanceUID)
        {
            return await ModifyDicomTag(modifyTag, "SeriesInstanceUID", seriesInstanceUID);
        }

        /// <summary>
        ///     Modify image tag where in SopInstanceUID
        /// </summary>
        [HttpPost("sopInstanceUID/{sopInstanceUID}")]
        public async Task<ActionResult> ModifyTagSopInstanceUID(
            [FromBody] ModifyDicomTagData modifyTag, string sopInstanceUID)
        {
            return await ModifyDicomTag(modifyTag, "SOPInstanceUID", sopInstanceUID);
        }

        private async Task<ActionResult> ModifyDicomTag(ModifyDicomTagData modifyTag, string key, string value)
        {
            try
            {
                var result = await _dicomTagService.ModifyTag(
                    User.Identity.Name,
                    key,
                    value,
                    modifyTag.Group,
                    modifyTag.Element,
                    modifyTag.Value,
                    _dicomOperationNodeService.GetLocalCStoreNode());
                return result ? Ok() : BadRequest("Unexpected error");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            
        }
    }
}