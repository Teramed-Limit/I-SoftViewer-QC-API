using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;


namespace ISoftViewerLibrary.Services.RepositoryService.Table
{
    public class DicomPatientService : CommonRepositoryService<DicomPatientData>
    {
        public DicomPatientService(PacsDBOperationService dbOperator)
            : base("DicomPatient", dbOperator)
        {
            PrimaryKey = "PatientId";
        }
    }

    public class DicomStudyService : CommonRepositoryService<DicomStudyData>
    {
        public DicomStudyService(PacsDBOperationService dbOperator)
            : base("DicomStudy", dbOperator)
        {
            PrimaryKey = "StudyInstanceUID";
        }
    }

    public class DicomSeriesService : CommonRepositoryService<DicomSeriesData>
    {
        public DicomSeriesService(PacsDBOperationService dbOperator)
            : base("DicomSeries", dbOperator)
        {
            PrimaryKey = "SeriesInstanceUID";
        }
    }

    public class DicomImageService : CommonRepositoryService<DicomImageData>
    {
        public DicomImageService(PacsDBOperationService dbOperator)
            : base("DicomImage", dbOperator)
        {
            PrimaryKey = "SOPInstanceUID";
        }
    }
}