using ISoftViewerLibrary.Services.RepositoryService.Table;

namespace ISoftViewerLibrary.Logics.Interface
{
    /// <summary>
    ///     QC操作紀錄
    /// </summary>
    public interface IQCOperationLoggerStrategy
    {
        void InjectService(OperationRecordService operationRecordService, DicomStudyService dicomStudyService);
        bool WriteSuccessRecord();
        bool WriteFailedRecord();
        void SetParams(string user, string studyInstanceUID, string reason, string desc);
        void SetReasonAndDesc(string reason, string desc);
    }
}