using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerQCSystem.Services;

namespace ISoftViewerQCSystem.Interfaces
{
    /// <summary>
    ///     QC操作紀錄
    /// </summary>
    public interface IQCOperationLoggerStrategy
    {
        bool WriteSuccessRecord(OperationRecordService operationRecordService);
        bool WriteFailedRecord(OperationRecordService operationRecordService);
        void SetParams(string user, string studyInstanceUID, string reason, string desc);
    }
}