using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class QCAutoMappingConfigService : CommonRepositoryService<QCAutoMappingConfig>
    {
        public QCAutoMappingConfigService(PacsDBOperationService dbOperator)
            : base("QC_AutoMappingConfig", dbOperator)
        {
            PrimaryKey = "StationName";
        }


        public List<DicomNode> GetPACSServiceProviderByStation(string stationName)
        {
            var where = new List<PairDatas>
            {
                new() { Name = "StationName", Value = stationName }
            };

            var columns = new List<PairDatas>
            {
                new() { Name = "StoreSCP" },
            };

            return DbOperator
                .BuildNoneQueryTable(TableName, where, columns)
                .Query<QCAutoMappingConfig>()
                .Select(x => JsonSerializer.Deserialize<List<DicomNode>>(x.StoreSCP, new JsonSerializerOptions()))
                .First();
        }
    }
}