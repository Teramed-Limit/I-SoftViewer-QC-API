using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerLibrary.Services.RepositoryService.View
{
    public class DicomPatientStudyViewService : CommonRepositoryService<SearchPatientStudyView>
    {
        public DicomPatientStudyViewService(PacsDBOperationService dbOperator)
            : base("SearchPatientStudyView", dbOperator)
        {
            PrimaryKey = "StudyInstanceUID";
        }

        public IEnumerable<SearchPatientStudyView> Query(Queries.V1.QueryDBKeys queryParams)
        {
            var where = (from property in queryParams.GetType().GetProperties()
                select property.Name
                into name
                let value = queryParams.GetType().GetProperty(name)?.GetValue(queryParams, null)?.ToString()
                where !string.IsNullOrEmpty(value)
                select new PairDatas { Name = name, Value = value }).ToList();

            return Get(where);
        }
    }
}