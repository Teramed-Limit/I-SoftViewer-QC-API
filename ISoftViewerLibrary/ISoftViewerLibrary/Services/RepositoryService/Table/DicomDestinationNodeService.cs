using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using System.Collections.Generic;
using System.Linq;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class DicomDestinationNodeService : CommonRepositoryService<DicomDestinationNode>
    {
        private readonly EnvironmentConfiguration _config;

        public DicomDestinationNodeService(PacsDBOperationService dbOperator, EnvironmentConfiguration config)
            : base("DicomDestinationNodes", dbOperator)
        {
            PrimaryKey = "LogicalName";
            _config = config;
        }
    }
}