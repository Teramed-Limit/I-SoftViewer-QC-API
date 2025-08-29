using System.Collections.Generic;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerQCSystem.Models
{
    /// <summary>
    ///     批量修改 DICOM Tag 的資料模型
    /// </summary>
    public class BatchModifyDicomTagData
    {
        /// <summary>
        ///     要修改的 Tag 清單
        /// </summary>
        public List<ModifyDicomTagData> Tags { get; set; } = new List<ModifyDicomTagData>();
    }
}