using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class OperationRecordService : CommonRepositoryService<OperationRecord>
    {
        public OperationRecordService(PacsDBOperationService dbOperator)
            : base("OperationRecord", dbOperator)
        {
            PrimaryKey = "Id";
        }
    }
}