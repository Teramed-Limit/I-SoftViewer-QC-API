using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.View
{
    public class QCOperationRecordViewService : CommonRepositoryService<QCOperationRecordView>
    {
        public QCOperationRecordViewService(PacsDBOperationService dbOperator)
            : base("QCOperationRecordView", dbOperator)
        {
            PrimaryKey = "Id";
        }
    }
}