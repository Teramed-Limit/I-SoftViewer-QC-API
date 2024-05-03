using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     修改Dicom Tag
    /// </summary>
    public class ModifyTagLogger : BaseOperationLogger
    {
        public ModifyTagLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.ModifyTag.ToString(),
                OperationName = "Modify Tag",
                Operator = "",
                Description = "Retrieve Study",
                Reason = "",
                QCGuid = "",
            };
        }
    }
}