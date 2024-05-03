using System;
using ISoftViewerLibrary.Models.DTOs;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     Send to specify PACS
    /// </summary>
    public class SendToPacsLogger : BaseOperationLogger
    {
        public SendToPacsLogger()
        {
            OperationRecord = new OperationRecord()
            {
                DateTime = DateTime.Now.ToString(),
                Operation = IQCOperationType.SendToPacs.ToString(),
                OperationName = "Send PACS",
                Operator = "",
                Description = "",
                Reason = "",
                QCGuid = ""
            };
        }
    }
}