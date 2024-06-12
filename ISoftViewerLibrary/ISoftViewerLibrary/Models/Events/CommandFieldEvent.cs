using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.Events
{
    public static class CommandFieldEvent
    {
        public class DbFieldCreated
        {
            /// <summary>
            /// 識別碼
            /// </summary>
            public Guid DbFieldCreateId { get; set; }
            /// <summary>
            /// 欄位名稱
            /// </summary>
            public string FieldName { get; set; }
            /// <summary>
            /// 欄位型態
            /// </summary>
            public FieldType Type { get; set; }
            /// <summary>
            /// 是否為主鍵
            /// </summary>
            public bool IsPrimaryKey { get; set; }
            /// <summary>
            /// 是否可以為空值
            /// </summary>
            public bool IsNull { get; set; }
            /// <summary>
            /// 執行Update語法是否要略過此欄位
            /// </summary>
            public bool UpdateSqlByPass { get; set; }
            /// <summary>
            /// 該欄位是否支援全文檢索查詢        
            /// </summary>
            public bool IsSupportFullTextSearch { get; set; }
            /// <summary>
            /// 用來做資料表查詢的運算子
            /// </summary>
            public FieldOperator SqlOperator { get; set; }
            /// <summary>
            /// 排序
            /// </summary>
            public OrderOperator OrderOperator { get; set; }
        }

        public class DbFieldChanged
        {
            /// <summary>
            /// 資料內容
            /// </summary>
            public string Value { get; set; }
            /// <summary>
            /// 第二組資料內容,通常用來做Between的End資料用
            /// </summary>
            public string Value2nd { get; set; }
            /// <summary>
            /// 二進位資料內容
            /// </summary>
            public byte[] BinaryValue { get; set; }
        }

        public class DicomFieldCreated
        {
            /// <summary>
            /// 識別碼
            /// </summary>
            public Guid DcmFieldCreateId { get; set; }
            /// <summary>
            /// DICOM Tag - Group
            /// </summary>
            public ushort DicomGroup { get; set; }
            /// <summary>
            /// DICOM Tag - Elem
            /// </summary>
            public ushort DicomElem { get; set; }
            /// <summary>
            /// 資料內容
            /// </summary>
            public string Value { get; set; }
        }

        public class DicomValueChanged
        {
            /// <summary>
            /// 資料內容
            /// </summary>
            public string Value { get; set; }
        }

        public class UpdateDbValueAndOrder
        {
            /// <summary>
            /// 資料內容
            /// </summary>
            public string Value { get; set; }
            /// <summary>
            /// 排序
            /// </summary>
            public OrderOperator OrderOperator { get; set; }
        }

        public class OnCreateDbUser
        {
            /// <summary>
            /// 建立使用者
            /// </summary>
            public string CreateUser { get; set; }
            /// <summary>
            /// 建立日期時間
            /// </summary>
            public string CreateDateTime { get; set; }
        }

        public class OnModifiedDbUser
        {
            /// <summary>
            /// 修改使用者
            /// </summary>
            public string ModifiedUser { get; set; }
            /// <summary>
            /// 修改日期時間
            /// </summary>
            public string ModifiedDateTime { get; set; }
        }

        public class OnPatientUidCreated
        {
            /// <summary>
            /// 病歷號碼
            /// </summary>
            public string PatientID { get; set; }
            /// <summary>
            /// 更新Set欄位用的PatientID
            /// </summary>
            public string UpdatedPatientID { get; set; }
        }

        public class OnPatientUidUpdated
        {
            /// <summary>
            /// 更新Set欄位用的PatientID
            /// </summary>
            public string UpdatedPatientID { get; set; }
            /// <summary>
            /// 修改人員
            /// </summary>
            public string ModifiedUser { get; set; }
        }

        public class OnStudyUidTableCreated
        {
            /// <summary>
            /// 要Update的StudyInstanceUID
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// 要當Update Where的StudyInstanceUID
            /// </summary>
            public string UpdateStudyInstanceUID { get; set; }
            /// <summary>
            /// 最原始的StudyInstanceUID
            /// </summary>
            public string ReferencedStudyInstanceUID { get; set; }
        }

        public class OnStudyUidUpdated
        {
            /// <summary>
            /// 檢查唯一碼
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// 更新檢查唯一碼
            /// </summary>
            public string UpdateStudyInstanceUID { get; set; }
            /// <summary>
            /// 修改人員
            /// </summary>
            public string ModifiedUser { get; set; }
        }

        public class OnStudyReferenceUidUpdated
        {
            /// <summary>
            /// 更新檢查唯一碼
            /// </summary>
            public string UpdateStudyInstanceUID { get; set; }
            /// <summary>
            /// 原生的檢查唯一碼
            /// </summary>
            public string ReferencedStudyInstanceUID { get; set; }
            /// <summary>
            /// 修改人員
            /// </summary>
            public string ModifiedUser { get; set; }
        }                

        public class OnSeriesUidCreated
        {
            /// <summary>
            /// 要Update的SeriesInstanceUID
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// 要當Update Where的SeriesInstanceUID
            /// </summary>
            public string UpdateSeriesInstanceUID { get; set; }
            /// <summary>
            /// 要Update的StudyInstanceUID
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// 原始的StudyInstanceUID
            /// </summary>
            public string ReferencedStudyInstanceUID { get; set; }
            /// <summary>
            /// 原始的SeriesInstanceUID
            /// </summary>
            public string ReferencedSeriesInstanceUID { get; set; }
        }

        public class OnSeriesReferenceUidUpdated
        {
            /// <summary>
            /// 原始SeriesInstanceUID
            /// </summary>
            public string ReferencedSeriesInstanceUID { get; set; }
            /// <summary>
            /// 原始StudyInstanceUID
            /// </summary>
            public string ReferencedStudyInstanceUID { get; set; }
        }

        public class OnSeriesUidUpdated
        {
            /// <summary>
            /// 要當Update Where的SeriesInstanceUID
            /// </summary>
            public string UpdateSeriesInstanceUID { get; set; }
            /// <summary>
            /// 要Update的StudyInstanceUID
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// 修改使用者
            /// </summary>
            public string ModifiedUser { get; set; }
        }

        public class OnImageUidCreated
        {
            /// <summary>
            /// 目前的SOPInstanceUID
            /// </summary>
            public string SOPInstanceUID { get; set; }
            /// <summary>
            /// 當Update的WhereSOPInstanceUID
            /// </summary>
            public string UpdateSOPInstanceUID { get; set; }
            /// <summary>
            /// 上層Series Instance UID
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// 原始的SOPInstanceUID
            /// </summary>
            public string ReferencedSOPInstanceUID { get; set; }
            /// <summary>
            /// 原始的SeriesInstanceUID
            /// </summary>
            public string ReferencedSeriesInstanceUID { get; set; }
            /// <summary>
            /// 裝置的編號
            /// </summary>
            public string StorageDeviceID { get; set; }
            /// <summary>
            /// 檔案儲存位置
            /// </summary>
            public string FilePath { get; set; }
            /// <summary>
            /// 未更新原始資料
            /// </summary>
            public string UnmappedDcmTags { get; set; }
        }

        public class OnImageUidAndFilePathUpdated
        {
            /// <summary>
            /// 更改後SeriesInstanceUID
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// Where條件的SOPInstanceUID
            /// </summary>
            public string UpdateSOPInstanceUID { get; set; }
            /// <summary>
            /// 檔案路徑
            /// </summary>
            public string FilePath { get; set; }
            /// <summary>
            /// 修改使用者
            /// </summary>
            public string ModifiedUser { get; set;}
        }
        
        public class OnImageUidUpdated
        {
            /// <summary>
            /// 更改後SeriesInstanceUID
            /// </summary>
            public string SeriesInstanceUID { get; set; }
            /// <summary>
            /// Where條件的SOPInstanceUID
            /// </summary>
            public string UpdateSOPInstanceUID { get; set; }
        }

        public class OnImageFilePathUpdated
        {
            /// <summary>
            /// 檔案儲存位置
            /// </summary>
            public string FilePath { get; set; }
        }

        public class OnImageReferenceUidUpdated
        {
            /// <summary>
            /// 原始的SOPInstanceUID
            /// </summary>
            public string ReferencedSOPInstanceUID { get; set; }
            /// <summary>
            /// 原始的SeriesInstanceUID
            /// </summary>
            public string ReferencedSeriesInstanceUID { get; set; }
        }

        public class OnImageUnmappedDcmTagUpdated
        {
            /// <summary>
            /// 未更新原始資料
            /// </summary>
            public string UnmappedDcmTags { get; set; }
        }

        public enum StudyMaintainType
        {
            Merged,
            Mapped,
            Deleted,
        }

        public class OnStudyStatusUpdate
        {
            /// <summary>
            /// 要Update的StudyInstanceUID
            /// </summary>
            public string StudyInstanceUID { get; set; }
            /// <summary>
            /// 操作種類
            /// </summary>
            public StudyMaintainType StudyMaintainType { get; set; }
            /// <summary>
            /// 操作狀態
            /// </summary>
            public string Value { get; set; }
        }
    }
}
