using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     匯入Dcm,Jpg影像檢查，dcm檔案不會改StudyInsUid所以可能有多筆
    /// </summary>
    public class ImportStudyLogger : BaseOperationLogger
    {
        public ImportStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.ImportStudy.ToString(),
                OperationName = "Import Study",
                Operator = "",
                Description = "Save to database",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}