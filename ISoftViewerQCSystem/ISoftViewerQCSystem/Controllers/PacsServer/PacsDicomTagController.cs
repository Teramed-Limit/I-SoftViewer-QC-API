using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsDicomTagController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="tagFilterDetail"></param>
        /// <param name="tagFileters"></param>
        public PacsDicomTagController(ICommonRepositoryService<SvrDcmTags> tag, ICommonRepositoryService<SvrDcmTagFilterDetail> tagFilterDetail,
            ICommonRepositoryService<SvrDcmTagFilters> tagFileters)
        {
            PacsDicomTagService = (DbTableService<SvrDcmTags>)tag;
            PacsDicomTagFilterDetaulService = (DbTableService<SvrDcmTagFilterDetail>)tagFilterDetail;
            PacsDicomTagFiltersService = (DbTableService<SvrDcmTagFilters>)tagFileters;
        }

        #region Fields        
        /// <summary>
        /// DicomTags資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmTags> PacsDicomTagService;
        /// <summary>
        /// DicomTagFilterDetail資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmTagFilterDetail> PacsDicomTagFilterDetaulService;
        /// <summary>
        /// DicomTagFilters資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrDcmTagFilters> PacsDicomTagFiltersService;
        #endregion

        #region Methods

        #region DicomTags
        /// <summary>
        /// 取得Dicom Tag列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("dcmtag")]
        public ActionResult<IEnumerable<SvrDcmTags>> GetDicomTag()
        {
            return Ok(PacsDicomTagService.GetAll());
        }
        /// <summary>
        /// 增加Dicom Tag     
        /// </summary>
        [HttpPost("dcmtag/identifyName")]
        public ActionResult AddDicomTag([FromBody] SvrDcmTags data)
        {
            var userName = User.Identity?.Name;

            data.IdentifyName = data.DicomGroup + "," + data.DicomElem;

            DicomOperatorHelper dcmOpHelper = new();
            dcmOpHelper.ConvertTagStringToUIntGE(data.IdentifyName, out ushort tagGroup, out ushort tagElem);
            DicomTag dTag = new(tagGroup, tagElem);
            data.TagName = dTag.ToString();

            if (PacsDicomTagService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 更新Dicom Tag
        /// </summary>
        [HttpPost("dcmtag/identifyName/{identifyName}")]
        public ActionResult PostDicomTag([FromBody] SvrDcmTags data, string identifyName)
        {
            var userName = User.Identity?.Name;

            data.IdentifyName = data.DicomGroup + "," + data.DicomElem;

            DicomOperatorHelper dcmOpHelper = new();
            dcmOpHelper.ConvertTagStringToUIntGE(data.IdentifyName, out ushort tagGroup, out ushort tagElem);
            DicomTag dTag = new(tagGroup, tagElem);
            data.TagName = dTag.ToString();

            if (PacsDicomTagService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 刪除Dicom Tag
        /// </summary>
        [HttpDelete("dcmtag/identifyName/{identifyName}")]
        public ActionResult DeleteDicomTag(string identifyName)
        {
            if (PacsDicomTagService.Delete(identifyName) == false)
                return BadRequest();

            return Ok();
        }
        #endregion

        #region DicomTagFilterDetail
        /// <summary>
        /// 取得Dicom Tag Filter Detail 列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("dcmtagDetail")]
        public ActionResult<IEnumerable<SvrDcmTagFilterDetail>> GetDicomTagFilterDetail()
        {
            return Ok(PacsDicomTagFilterDetaulService.GetAll());
        }
        /// <summary>
        /// 增加Dicom Tag Filter Detail
        /// </summary>
        [HttpPost("dcmtagDetail/tagFilterName")]
        public ActionResult AddDicomTagFilterDetail([FromBody] SvrDcmTagFilterDetail data)
        {
            var userName = User.Identity?.Name;

            if (PacsDicomTagFilterDetaulService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 更新Dicom Tag Filter Detail
        /// </summary>
        [HttpPost("dcmtagDetail/tagFilterName/{tagFilterName}")]
        public ActionResult PostDicomTagFilterDetail([FromBody] SvrDcmTagFilterDetail data, string tagFilterName)
        {
            var userName = User.Identity?.Name;

            if (PacsDicomTagFilterDetaulService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 刪除Dicom Tag
        /// </summary>
        [HttpDelete("dcmtagDetail/tagFilterName/{tagFilterName}")]
        public ActionResult DeleteDicomTagFilterDetail(string tagFilterName)
        {
            if (PacsDicomTagFilterDetaulService.Delete(tagFilterName) == false)
                return BadRequest();

            return Ok();
        }
        #endregion

        #region DicomTagFilters
        /// <summary>
        /// 取得Dicom Tag Filters 列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("dcmtagFilters")]
        public ActionResult<IEnumerable<SvrDcmTagFilters>> GetDicomTagFilters()
        {
            return Ok(PacsDicomTagFiltersService.GetAll());
        }
        /// <summary>
        /// 增加Dicom Tag Filter Filters
        /// </summary>
        [HttpPost("dcmtagFilters/tagFilterName")]
        public ActionResult AddDicomTagFilters([FromBody] SvrDcmTagFilters data)
        {
            var userName = User.Identity?.Name;

            if (PacsDicomTagFiltersService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 更新Dicom Tag Filter Detail
        /// </summary>
        [HttpPost("dcmtagFilters/tagFilterName/{tagFilterName}")]
        public ActionResult PostDicomTagFilters([FromBody] SvrDcmTagFilters data, string tagFilterName)
        {
            var userName = User.Identity?.Name;

            if (PacsDicomTagFiltersService.AddOrUpdate(data, userName) == false)
                return BadRequest();
            return Ok();
        }
        /// <summary>
        /// 刪除Dicom Tag
        /// </summary>
        [HttpDelete("dcmtagFilters/tagFilterName/{tagFilterName}")]
        public ActionResult DeleteDicomTagFilters(string tagFilterName)
        {
            if (PacsDicomTagFiltersService.Delete(tagFilterName) == false)
                return BadRequest();

            return Ok();
        }
        #endregion

        #endregion
    }
}
