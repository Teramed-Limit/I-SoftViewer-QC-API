using System;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class DicomPatientData : JsonDatasetBase
    {
        public string PatientId { get; set; }

        public string PatientsName { get; set; }

        public string PatientsSex { get; set; }

        public string PatientsBirthDate { get; set; }

        public string PatientsBirthTime { get; set; }

        public string OtherPatientNames { get; set; }

        public string OtherPatientId { get; set; }

        public string EthnicGroup { get; set; }

        public string PatientComments { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string DocumentNumber { get; set; }
    }

    public class DicomStudyData : JsonDatasetBase
    {
        public string StudyInstanceUID { get; set; }

        public string PatientId { get; set; }

        public string StudyDate { get; set; }

        public string StudyTime { get; set; }

        public string ReferringPhysiciansName { get; set; }

        public string StudyID { get; set; }

        public string AccessionNumber { get; set; }

        public string StudyDescription { get; set; }

        public string Modality { get; set; }

        public string PerformingPhysiciansName { get; set; }

        public string NameofPhysiciansReading { get; set; }

        public string StudyStatus { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string ProcedureID { get; set; }

        public string ReferencedStudyInstanceUID { get; set; }

        public int? Merged { get; set; }

        public int? Mapped { get; set; }

        public int? Deleted { get; set; }

        public string QCGuid { get; set; }
    }

    public class DicomSeriesData : JsonDatasetBase
    {
        public string SeriesInstanceUID { get; set; }

        public string StudyInstanceUID { get; set; }

        public string SeriesModality { get; set; }

        public string SeriesDate { get; set; }

        public string SeriesTime { get; set; }

        public string SeriesNumber { get; set; }

        public string SeriesDescription { get; set; }

        public string PatientPosition { get; set; }

        public string BodyPartExamined { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string ReferencedStudyInstanceUID { get; set; }

        public string ReferencedSeriesInstanceUID { get; set; }
    }

    public class DicomImageData : JsonDatasetBase
    {
        public string SOPInstanceUID { get; set; }

        public string SeriesInstanceUID { get; set; }

        public string SOPClassUID { get; set; }

        public string ImageNumber { get; set; }

        public string ImageDate { get; set; }

        public string ImageTime { get; set; }

        public string FilePath { get; set; }

        public string StorageDeviceID { get; set; }

        public string ImageStatus { get; set; }

        public int? WindowWidth { get; set; }

        public int? WindowCenter { get; set; }

        public string KeyImage { get; set; }

        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }

        public string HaveSendToRemote { get; set; }

        public string ReferencedSOPInstanceUID { get; set; }

        public string ReferencedSeriesInstanceUID { get; set; }
    }

    public class SearchImagePathView : JsonDatasetBase
    {
        // Path to the image file
        public string ImageFullPath { get; set; }

        // Unique identifier for the SOP instance
        public string SOPInstanceUID { get; set; }

        // Class UID of the SOP
        public string SOPClassUID { get; set; }

        // Number of the image
        public int ImageNumber { get; set; }

        // Date the image was taken
        public string ImageDate { get; set; }

        // Time the image was taken
        public string ImageTime { get; set; }

        // Path where the image file is stored
        public string FilePath { get; set; }

        // ID of the storage device
        public string StorageDeviceID { get; set; }

        // Status of the image
        public string ImageStatus { get; set; }

        // ID of the patient
        public string PatientId { get; set; }

        // Name of the patient
        public string PatientsName { get; set; }

        // Unique identifier for the study instance
        public string StudyInstanceUID { get; set; }

        // Date of the study
        public string StudyDate { get; set; }

        // Time of the study
        public string StudyTime { get; set; }

        // Accession number of the study
        public string AccessionNumber { get; set; }

        // Description of the study
        public string StudyDescription { get; set; }

        // Modality of the series
        public string SeriesModality { get; set; }

        // Part of the body examined
        public string BodyPartExamined { get; set; }

        // Position of the patient
        public string PatientPosition { get; set; }

        // Path to the storage location
        public string StoragePath { get; set; }

        // HTTP path to the file
        public string HttpFilePath { get; set; }

        // Description of the storage
        public string StorageDescription { get; set; }

        // Unique identifier for the series instance
        public string SeriesInstanceUID { get; set; }

        // Date and time of creation
        public string CreateDateTime { get; set; }

        // User who created this data
        public string CreateUser { get; set; }

        // Date and time of last modification
        public string ModifiedDateTime { get; set; }

        // User who last modified this data
        public string ModifiedUser { get; set; }

        public string Annotations { get; set; }

        public bool KeyImage { get; set; }
    }

    public class DicomImageDataDto : DicomImageData
    {
        public string DcmPath { get; set; }
        public string JpgPath { get; set; }
    }

    public class SearchImagePathViewDto: SearchImagePathView
    {
        // Number of the image
        public int ImageNumber { get; set; }
    }

    public class SearchPatientStudyView : JsonDatasetBase
    {
        public string PatientId { get; set; }

        public string PatientsName { get; set; }

        public string PatientsSex { get; set; }

        public string PatientsBirthDate { get; set; }

        public string PatientsBirthTime { get; set; }

        public string StudyInstanceUID { get; set; }

        public string StudyDate { get; set; }

        public string ReferringPhysiciansName { get; set; }

        public string AccessionNumber { get; set; }

        public string StudyDescription { get; set; }

        public string Modality { get; set; }

        public string PerformingPhysiciansName { get; set; }

        public int Merged { get; set; }

        public int Mapped { get; set; }

        public int Deleted { get; set; }

        public string QCGuid { get; set; }
    }
}