using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     Merge study資料
    /// </summary>
    public class SplitStudyLogger : BaseOperationLogger
    {
        public SplitStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.SplitStudy.ToString(),
                OperationName = "Spilt Study",
                Operator = "",
                Description = "",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}