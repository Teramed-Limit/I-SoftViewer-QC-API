using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class QcFunctionService : CommonRepositoryService<QCFunction>
    {
        public QcFunctionService(PacsDBOperationService dbOperator)
            : base("QCFunction", dbOperator)
        {
            PrimaryKey = "FunctionName";
        }
    }
}