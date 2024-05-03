using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     UnMapping Worklist資料
    /// </summary>
    public class UnMappingStudyLogger : BaseOperationLogger
    {
        public UnMappingStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.UnMappingStudy.ToString(),
                OperationName = "UnMapping Study",
                Operator = "",
                Description = "",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}