using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Events
{
    public static class DcmEvents
    {
        #region PatientEntityEvent    
        /// <summary>
        /// Patient層基本資料
        /// </summary>
        public class OnPatientUpdated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0010,0010)​
            /// </summary>
            public string PatientsName { get; set; }
            /// <summary>
            /// (0010,0040)
            /// </summary>
            public string PatientsSex { get; set; }
            /// <summary>
            /// (0010,0030)​
            /// </summary>
            public string PatientsBirthDate { get; set; }
            /// <summary>
            /// (0010,0032)
            /// </summary>
            public string PatientsBirthTime { get; set; }
            /// <summary>
            /// (0010,1001)​
            /// </summary>
            public string OtherPatientNames { get; set; }
            /// <summary>
            /// (0010,1000)​
            /// </summary>
            public string OtherPatientId { get; set; }
            /// <summary>
            /// 額外的DICOM Tag資料
            /// </summary>
            public List<DataCorrection.V1.DcmTagData> DcmOtherTags { get; set; }
        }
        /// <summary>
        /// DICOM Patient實體建立
        /// </summary>
        public class OnPatientCreated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0010,0020)​
            /// </summary>
            public string PatientId { get; set; }
            /// <summary>
            /// Patient一般資料
            /// </summary>
            public OnPatientUpdated NormalKeys { get; set; }
        }
        #endregion

        #region StudyEntityEvent    

        public class OnStudyUpdated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0008,0020)​
            /// </summary>
            public string StudyDate { get; set; }
            /// <summary>
            /// (0008,0030)​
            /// </summary>
            public string StudyTime { get; set; }
            /// <summary>
            /// (0008,0090)​
            /// </summary>
            public string ReferringPhysiciansName { get; set; }
            /// <summary>
            /// (0020,0010)​
            /// </summary>
            public string StudyID { get; set; }
            /// <summary>
            /// (0008,0050)​
            /// </summary>
            public string AccessionNumber { get; set; }
            /// <summary>
            /// (0008,1030)​
            /// </summary>
            public string StudyDescription { get; set; }
            /// <summary>
            /// (0008,0060)​
            /// </summary>
            public string Modality { get; set; }
            /// <summary>
            /// (0008,1050)​
            /// </summary>
            public string PerformingPhysiciansName { get; set; }
            /// <summary>
            /// 90008,1060)​
            /// </summary>
            public string NameofPhysiciansReading { get; set; }
            /// <summary>
            /// (0040,1001)​
            /// </summary>
            public string ProcedureID { get; set; }
            /// <summary>
            /// 額外的DICOM Tag資料
            /// </summary>
            public List<DataCorrection.V1.DcmTagData> DcmOtherTags { get; set; }
        }

        public class OnStudyCreated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0020,000D)
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// (0010,0020)​
            /// </summary>
            public string PatientId { get; set; }            

            public OnStudyUpdated NormalKeys { get; set; }
        }
        /// <summary>
        /// QC Study資料建立
        /// </summary>
        public class OnStudyQcUidCreated
        {
            public Guid Id { get; set; }

            public string StudyInstanceUID { get; set; }

            public string UpdateStudyInstanceUID { get; set; }

            public string ReferencedStudyInstanceUID { get; set; }
        }
        #endregion

        #region WorklistStudyEntityEvent
        public class OnScheduledProcedureStepCreated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0020,000D)
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// (0010,0020)​
            /// </summary>
            public string PatientId { get; set; }
            /// <summary>
            /// (0010,0010)​
            /// </summary>
            public string PatientName { get; set; }
            /// <summary>
            /// (0008,0050)​
            /// </summary>
            public string AccessionNumber { get; set; }
            /// <summary>
            /// (0040,0002)​ Scheduled Procedure Step Start Date
            /// </summary>
            public string StudyDate { get; set; }
            /// <summary>
            /// (0040,0006)​ Scheduled Performing Physician's Name
            /// </summary>
            public string PerformingPhysiciansName { get; set; }
            /// <summary>
            /// (0040,0007)​ Scheduled Procedure Step Description
            /// </summary>
            public string StudyDescription { get; set; }            
            /// <summary>
            /// (0008,0060)​
            /// </summary>
            public string Modality { get; set; }
            /// <summary>
            /// (0040,0009)​ Scheduled Procedure Step ID
            /// </summary>
            public string ProcedureID { get; set; }
        }
        #endregion

        #region SeriesEntity    

        public class OnSeriesUpdated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0008,0060)​
            /// </summary>
            public string SeriesModality { get; set; }
            /// <summary>
            /// (0008,0021)
            /// </summary>
            public string SeriesDate { get; set; }
            /// <summary>
            /// (0008,0031)
            /// </summary>
            public string SeriesTime { get; set; }
            /// <summary>
            /// (0020,0011)​
            /// </summary>
            public string SeriesNumber { get; set; }
            /// <summary>
            /// (0008,103E)​
            /// </summary>
            public string SeriesDescription { get; set; }
            /// <summary>
            /// (0018,5100)
            /// </summary>
            public string PatientPosition { get; set; }
            /// <summary>
            /// (0018,0015)​
            /// </summary>
            public string BodyPartExamined { get; set; }
            /// <summary>
            /// 額外的DICOM Tag資料
            /// </summary>
            public List<DataCorrection.V1.DcmTagData> DcmOtherTags { get; set; }
        }

        public class OnSeriesCreated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0020,000E)​
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// (0020,000D)
            /// </summary>
            public string StudyInstanceUID { get; set; }

            public OnSeriesUpdated NormalKeys { get; set; }
        }

        public class OnSeriesQcUidCreated
        {
            public string SeriesInstanceUID { get; set; }

            public string UpdateSeriesInstanceUID { get; set; }

            public string StudyInstanceUID { get; set; }

            public string ReferencedStudyInstanceUID { get; set; }

            public string ReferencedSeriesInstanceUID { get; set; }
        }
        #endregion

        #region ImageEntityEvent   

        public class OnImageUpdated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0020,0013)​
            /// </summary>
            public string ImageNumber { get; set; }
            /// <summary>
            /// (0008,0023)​
            /// </summary>
            public string ImageDate { get; set; }
            /// <summary>
            /// (0008,0033)​
            /// </summary>
            public string ImageTime { get; set; }
            /// <summary>
            /// (0028,1051)​
            /// </summary>
            public string WindowWidth { get; set; }
            /// <summary>
            /// (0028,1050)​
            /// </summary>
            public string WindowCenter { get; set; }
        }

        public class OnImageBufferUpdated
        {
            public byte[] ImageBuffer { get; set; }
        }

        public class OnImageCreated
        {
            public Guid Id { get; set; }
            /// <summary>
            /// (0008,0018)​
            /// </summary>
            public string SOPInstanceUID { get; set; }
            /// <summary>
            /// (0020,000E)
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// (0008,0016)
            /// </summary>
            public string SOPClassUID { get; set; }
            /// <summary>
            /// 是否為DICOM檔案
            /// </summary>
            public bool IsDcmBuffer { get; set; }

            public OnImageUpdated NormalKeys { get; set; }

            public OnImageBufferUpdated ImageBuffer { get; set; }
        }

        public class OnImageQcUidCreated
        {
            public string SOPInstanceUID { get; set; }

            public string UpdateSOPInstanceUID { get; set; }

            public string SeriesInstanceUID { get; set; }

            public string ReferencedSOPInstanceUID { get; set; }

            public string ReferencedSeriesInstanceUID { get; set; }

            public string StorageDeviceID { get; set; }

            public string FilePath { get; set; }
        }
        #endregion
    }
}
