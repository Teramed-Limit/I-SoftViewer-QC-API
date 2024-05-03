using System;

namespace ISoftViewerLibrary.Models.DTOs
{
    /// <summary>
    ///     QC操作紀錄類型
    /// </summary>
    public enum IQCOperationType
    {
        ImportStudy,
        RetrieveStudy,
        ModifyTag,
        SendToPacs,
        MappingStudy,
        UnMappingStudy,
        MergeStudy,
        SplitStudy,
    }

    public class OperationRecord : JsonDatasetBase
    {
        public string DateTime { get; set; }

        public string Operation { get; set; }

        public string OperationName { get; set; }

        public string Description { get; set; }

        public string Operator { get; set; }

        public string Reason { get; set; }

        public string QCGuid { get; set; }

        public int IsSuccess { get; set; }
    }

    public class QCOperationRecordView : JsonDatasetBase
    {
        public string PatientId { get; set; }

        public string StudyDate { get; set; }

        public string AccessionNumber { get; set; }

        public string Modality { get; set; }

        public string StudyDescription { get; set; }

        public string QCGuid { get; set; }

        public int IsSuccess { get; set; }
    }

}