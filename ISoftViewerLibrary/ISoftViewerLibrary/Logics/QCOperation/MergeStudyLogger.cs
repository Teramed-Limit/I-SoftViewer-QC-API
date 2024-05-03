using System;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Table;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     Merge study資料
    /// </summary>
    public class MergeStudyLogger : BaseOperationLogger
    {
        public MergeStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.MergeStudy.ToString(),
                OperationName = "Merge Study",
                Operator = "",
                Description = "",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}