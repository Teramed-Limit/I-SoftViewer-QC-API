using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     Mapping Worklist資料
    /// </summary>
    public class MappingStudyLogger : BaseOperationLogger
    {
        public MappingStudyLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.MappingStudy.ToString(),
                OperationName = "Mapping Study",
                Operator = "",
                Description = "",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}