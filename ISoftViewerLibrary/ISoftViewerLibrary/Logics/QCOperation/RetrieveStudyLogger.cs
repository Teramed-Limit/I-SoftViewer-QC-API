using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     C-Move操作
    /// </summary>
    public class RetrieveStudyLogger : BaseOperationLogger
    {
        public RetrieveStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.RetrieveStudy.ToString(),
                OperationName = "Retrieve Study",
                Operator = "",
                Description = "Retrieved Study",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}