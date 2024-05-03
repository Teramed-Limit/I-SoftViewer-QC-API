using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibrary.Models.Interface
{
    #region IDcmQueries
    /// <summary>
    /// DICOM查詢服務
    /// </summary>
    public interface IDcmQueries : IOpMessage
    {
        /// <summary>
        /// 查詢Dicom資料
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <param name="type"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        Task<Queries.V1.QueryResult> FindDataJson(DicomIODs dicomIODs, DcmServiceUserType type, string host, int port, string callingAe, 
            string calledAe, Dictionary<string, object> parameter);
        /// <summary>
        /// 查詢資料庫資料
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<Queries.V1.QueryResult> FindDataJson(DicomIODs dicomIODs, DbSearchResultType type);
    }
    #endregion

    #region IDcmCommand
    /// <summary>
    /// DICOM CRS服務
    /// </summary>
    public interface IDcmCommand : IOpMessage
    {
        /// <summary>
        /// 更新服務
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        Task<bool> Update(DicomIODs dicomIODs, DcmServiceUserType type, List<DicomOperationNodes> nodesList);
    }
    #endregion
}
