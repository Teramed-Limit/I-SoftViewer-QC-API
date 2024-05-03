using Dicom;
using ISoftViewerLibrary.Models.Aggregate;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IDcmCqusDatasets
    /// <summary>
    /// DICOM CQUS所使用的DicomDataset集合
    /// </summary>
    public interface IDcmCqusDatasets
    {
        /// <summary>
        /// 處理的DICOM列表
        /// </summary>
        ConcurrentBag<DicomDataset> DicomDatasets { get; }
    }
    #endregion

    #region IDcmRepository
    /// <summary>
    /// DICOM處理庫
    /// </summary>
    public interface IDcmRepository : IDcmCqusDatasets, IOpMessage
    {        
        /// <summary>
        /// DICOM檔案及資料封裝
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        Task<bool> DcmDataEncapsulation(DicomIODs dicomIODs, DcmServiceUserType type, Dictionary<string, object> parameter);        
    }
    #endregion
}
