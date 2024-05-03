using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerQCSystem.Models;
using ISoftViewerQCSystem.Services.Interface;
using static ISoftViewerLibrary.Models.DTOs.DcmNode.PACS;

namespace ISoftViewerQCSystem.Services
{
    public class DicomNodeService : CommonRepositoryService<DicomNode>
    {
        public DicomNodeService(PacsDBOperationService dbOperator)
            : base("DicomNodes", dbOperator)
        {
            PrimaryKey = "Name";
        }
    }
}