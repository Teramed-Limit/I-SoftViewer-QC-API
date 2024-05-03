using System.Collections.Generic;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.View
{
    public class DicomImagePathViewService : CommonRepositoryService<SearchImagePathView>
    {
        public DicomImagePathViewService(PacsDBOperationService dbOperator)
            : base("SearchImagePathView", dbOperator)
        {
            PrimaryKey = "SOPInstanceUID";
        }
    }
}