using Dicom;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Interfaces
{
    #region IDcmDataWrapper
    /// <summary>
    /// DICOM資料封裝器
    /// </summary>
    public interface IDcmDataWrapper<T1>
    {
        /// <summary>
        /// 資料封裝
        /// </summary>
        /// <param name="dicomFile"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool DataWrapper(DicomDataset dcmDataset, T1 entity, DcmString key);        
        /// <summary>
        /// 資料封裝
        /// </summary>
        /// <param name="dicomFile"></param>
        /// <param name="entities"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        bool DataWrapper(DicomDataset dcmDataset, Func<DcmString, T1> getEntityFunc, Func<T1, DcmString> GetUiqueKeyFunc, DcmString selfKey);
    }
    #endregion

    
}
