using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class StaticOptionsService : CommonRepositoryService<StaticOption>
    {
        public StaticOptionsService(PacsDBOperationService dbOperator)
            : base("StaticOptions", dbOperator)
        {
            PrimaryKey = "Id";
            IsIdentityPrimaryKey = true;
        }
    }
}