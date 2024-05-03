using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerLibrary.Models.DTOs
{
    public static class Queries
    {
        public static class V1
        {
            /// <summary>
            ///     查詢目的類型
            /// </summary>
            public enum QueryTargetType
            {
                qttDatabase = 0,
                qttQR = 1,
                qttWorklist = 2,
                qttHIS = 3
            }


            #region QueryDBKeys
            /// <summary>
            ///     查詢條件
            /// </summary>
            public class QueryDBKeys
            {
                #region Patient Level

                /// <summary>
                ///     Patient層欄位
                /// </summary>
                public string PatientId { get; set; }

                /// <summary>
                ///     病人姓名
                /// </summary>
                public string PatientsName { get; set; }

                /// <summary>
                ///     性別
                /// </summary>
                public string PatientsSex { get; set; }

                /// <summary>
                ///     出生日期
                /// </summary>
                public string PatientsBirthDate { get; set; }

                /// <summary>
                ///     出生時間
                /// </summary>
                public string PatientsBirthTime { get; set; }

                /// <summary>
                ///     其它病人姓名
                /// </summary>
                public string OtherPatientNames { get; set; }

                /// <summary>
                ///     其它病歷號碼
                /// </summary>
                public string OtherPatientId { get; set; }

                #endregion

                #region Study Level

                /// <summary>
                ///     檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }

                /// <summary>
                ///     檢查日期
                /// </summary>
                public string StudyDate { get; set; }

                /// <summary>
                ///     檢查時間
                /// </summary>
                public string StudyTime { get; set; }

                /// <summary>
                ///     主治醫師
                /// </summary>
                public string ReferringPhysiciansName { get; set; }

                /// <summary>
                ///     檢查編號
                /// </summary>
                public string StudyID { get; set; }

                /// <summary>
                ///     檢查單號
                /// </summary>
                public string AccessionNumber { get; set; }

                /// <summary>
                ///     檢查說明
                /// </summary>
                public string StudyDescription { get; set; }

                /// <summary>
                ///     儀器種類
                /// </summary>
                public string Modality { get; set; }

                /// <summary>
                ///     執行醫師姓名
                /// </summary>
                public string PerformingPhysiciansName { get; set; }

                /// <summary>
                ///     報告醫師姓名
                /// </summary>
                public string NameofPhysiciansReading { get; set; }

                /// <summary>
                ///     檢查程序代碼
                /// </summary>
                public string ProcedureID { get; set; }

                #endregion
            }
            #endregion

            #region FindWorklistKeys

            /// <summary>
            ///     查詢條件
            /// </summary>
            public class FindWorklistKeys
            {
                /// <summary>
                ///     建構
                /// </summary>
                public FindWorklistKeys()
                {
                    QueryName = "";
                }

                #region Fields

                #region Patient Level

                /// <summary>
                ///     Patient層欄位
                /// </summary>
                public string PatientId { get; set; }

                /// <summary>
                ///     病人姓名
                /// </summary>
                public string PatientsName { get; set; }

                /// <summary>
                ///     性別
                /// </summary>
                public string PatientsSex { get; set; }

                /// <summary>
                ///     出生日期
                /// </summary>
                public string PatientsBirthDate { get; set; }

                /// <summary>
                ///     出生時間
                /// </summary>
                public string PatientsBirthTime { get; set; }

                /// <summary>
                ///     其它病人姓名
                /// </summary>
                public string OtherPatientNames { get; set; }

                /// <summary>
                ///     其它病歷號碼
                /// </summary>
                public string OtherPatientId { get; set; }

                #endregion

                #region Study Level

                /// <summary>
                ///     檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }

                /// <summary>
                ///     檢查日期
                /// </summary>
                public string StudyDate { get; set; }

                /// <summary>
                ///     檢查時間
                /// </summary>
                public string StudyTime { get; set; }

                /// <summary>
                ///     主治醫師
                /// </summary>
                public string ReferringPhysiciansName { get; set; }

                /// <summary>
                ///     檢查編號
                /// </summary>
                public string StudyID { get; set; }

                /// <summary>
                ///     檢查單號
                /// </summary>
                public string AccessionNumber { get; set; }

                /// <summary>
                ///     檢查說明
                /// </summary>
                public string StudyDescription { get; set; }

                /// <summary>
                ///     儀器種類
                /// </summary>
                public string Modality { get; set; }

                /// <summary>
                ///     執行醫師姓名
                /// </summary>
                public string PerformingPhysiciansName { get; set; }

                /// <summary>
                ///     報告醫師姓名
                /// </summary>
                public string NameofPhysiciansReading { get; set; }

                /// <summary>
                ///     檢查程序代碼
                /// </summary>
                public string ProcedureID { get; set; }

                #endregion

                /// <summary>
                ///     查詢目的名稱編號
                /// </summary>
                [Required]
                public string QueryName { get; set; }

                #endregion
            }

            #endregion

            #region FindQRKeys

            public class FindQRKeys
            {
                public FindQRKeys()
                {
                    QueryName = "";
                }

                #region Fields

                #region Patient Level

                /// <summary>
                ///     Patient層欄位
                /// </summary>
                public string PatientId { get; set; }

                /// <summary>
                ///     病人姓名
                /// </summary>
                public string PatientsName { get; set; }

                /// <summary>
                ///     性別
                /// </summary>
                public string PatientsSex { get; set; }

                /// <summary>
                ///     出生日期
                /// </summary>
                public string PatientsBirthDate { get; set; }

                /// <summary>
                ///     出生時間
                /// </summary>
                public string PatientsBirthTime { get; set; }

                /// <summary>
                ///     其它病人姓名
                /// </summary>
                public string OtherPatientNames { get; set; }

                /// <summary>
                ///     其它病歷號碼
                /// </summary>
                public string OtherPatientId { get; set; }

                #endregion

                #region Study Level

                /// <summary>
                ///     檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }

                /// <summary>
                ///     檢查日期
                /// </summary>
                public string StudyDate { get; set; }

                /// <summary>
                ///     檢查時間
                /// </summary>
                public string StudyTime { get; set; }

                /// <summary>
                ///     主治醫師
                /// </summary>
                public string ReferringPhysiciansName { get; set; }

                /// <summary>
                ///     檢查編號
                /// </summary>
                public string StudyID { get; set; }

                /// <summary>
                ///     檢查單號
                /// </summary>
                public string AccessionNumber { get; set; }

                /// <summary>
                ///     檢查說明
                /// </summary>
                public string StudyDescription { get; set; }

                /// <summary>
                ///     儀器種類
                /// </summary>
                public string Modality { get; set; }

                /// <summary>
                ///     執行醫師姓名
                /// </summary>
                public string PerformingPhysiciansName { get; set; }

                /// <summary>
                ///     報告醫師姓名
                /// </summary>
                public string NameofPhysiciansReading { get; set; }

                /// <summary>
                ///     檢查程序代碼
                /// </summary>
                public string ProcedureID { get; set; }

                #endregion

                #region Series Level

                /// <summary>
                ///     系列唯一碼
                /// </summary>
                public string SeriesInstanceUID { get; set; }

                /// <summary>
                ///     儀器種類
                /// </summary>
                public string SeriesModality { get; set; }

                /// <summary>
                ///     系列日期
                /// </summary>
                public string SeriesDate { get; set; }

                /// <summary>
                ///     系列時間
                /// </summary>
                public string SeriesTime { get; set; }

                /// <summary>
                ///     系列編號
                /// </summary>
                public string SeriesNumber { get; set; }

                /// <summary>
                ///     系列說明
                /// </summary>
                public string SeriesDescription { get; set; }

                /// <summary>
                ///     病人拍攝位置
                /// </summary>
                public string PatientPosition { get; set; }

                /// <summary>
                ///     拍攝部位
                /// </summary>
                public string BodyPartExamined { get; set; }

                #endregion

                /// <summary>
                ///     查詢目的名稱編號
                /// </summary>
                [Required]
                public string QueryName { get; set; }

                #endregion
            }

            #endregion

            #region MoveQRKeys

            public class MoveQRKeys
            {
                #region Fields

                [Required]
                public string PatientId { get; set; }

                #region StudyLevel

                /// <summary>
                ///     檢查唯一碼
                /// </summary>
                [Required]
                public string StudyInstanceUID { get; set; }

                #endregion

                #region SeriesLevel

                /// <summary>
                ///     系列唯一碼
                /// </summary>
                public string SeriesInstanceUID { get; set; }

                #endregion

                /// <summary>
                ///     查詢目的名稱編號
                /// </summary>
                [Required]
                public string QueryName { get; set; }

                #endregion
            }

            #endregion

            #region QueryResult

            public class QueryResult
            {
                public QueryResult()
                {
                    Datasets = new List<List<DcmTagData>>();
                    FileSetIDs = new List<DcmTagData>();
                }

                #region Fields

                /// <summary>
                ///     資料集合
                /// </summary>
                public List<List<DcmTagData>> Datasets { get; set; }

                /// <summary>
                ///     檔案集合 File-Set IDs(0004,1130)
                /// </summary>
                public List<DcmTagData> FileSetIDs { get; set; }

                #endregion
            }

            #endregion

            #region CommandResult

            /// <summary>
            ///     命令執行結果
            /// </summary>
            public class CommandResult
            {
                public CommandResult()
                {
                    Resultes = new List<DcmTagData>();
                    ExecuteResult = false;
                }

                #region Fields

                /// <summary>
                ///     資料集合
                /// </summary>
                public List<DcmTagData> Resultes { get; set; }

                /// <summary>
                ///     處理結果,無資料回覆
                /// </summary>
                public bool ExecuteResult { get; set; }

                #endregion
            }

            #endregion
        }
    }
}