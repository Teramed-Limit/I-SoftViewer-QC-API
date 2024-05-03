using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs.PacsServer
{
    #region SvrDcmTags
    /// <summary>
    /// DICOM Tag列表
    /// </summary>
    public class SvrDcmTags : JsonDatasetBase
    {
        /// <summary>
        /// 識別碼(DicomGroup,DicomElem)
        /// </summary>                
        public string IdentifyName { get; set; }
        /// <summary>
        /// Dicom Tag Group
        /// </summary>
        [Required]
        public string DicomGroup { get; set; }
        /// <summary>
        /// Dicom Tag Elem
        /// </summary>
        [Required]
        public string DicomElem { get; set; }
        /// <summary>
        /// Tag名稱
        /// </summary>
        public string TagName { get; set; }
    }
    #endregion

    #region SvrDcmTagFilters
    /// <summary>
    /// Dicom Tag Filter主表
    /// </summary>
    public class SvrDcmTagFilters : JsonDatasetBase
    {
        /// <summary>
        /// Tag Filter識別名稱
        /// </summary>
        [Required]
        public string TagFilterName { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public string Description { get; set; }
    }
    #endregion

    #region SvrDcmTagFilterDetail
    /// <summary>
    /// Dicom Tag Filter明細表
    /// </summary>
    public class SvrDcmTagFilterDetail : JsonDatasetBase
    {
        /// <summary>
        /// Tag主表名稱
        /// </summary>
        public string TagFilterName { get; set; }
        /// <summary>
        /// 識別名稱
        /// </summary>
        public string TagIdentifyName { get; set; }
        /// <summary>
        /// Tag 規則, is, is not, contains, doesnt contain, replace
        /// </summary>
        public string TagRule { get; set; }
        /// <summary>
        /// 資料
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 目前尚未啟用
        /// </summary>
        public string AndAll { get; set; }
    }
    #endregion
}
