using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{
    public static class Log
    {
        public static class V1
        {
            /// <summary>
            /// 記錄器
            /// </summary>            
            public class Logger
            {
                /// <summary>
                /// 建構
                /// </summary>
                public Logger()
                {
                    UserID = "";
                    FunctionName = "";
                    OptContent1 = "";
                    OptContent2 = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="userID"></param>
                /// <param name="functionName"></param>
                /// <param name="opDate"></param>
                /// <param name="opTime"></param>
                /// <param name="optContent1"></param>
                /// <param name="optContent2"></param>
                public Logger(string userID, string functionName, string opDate, string opTime, string optContent1, string optContent2)
                {
                    UserID = userID;
                    FunctionName = functionName;
                    OpDate = opDate;
                    OpTime = opTime;
                    OptContent1 = optContent1;
                    OptContent2 = optContent2;
                }

                #region Fields
                /// <summary>
                /// 使用者帳號
                /// </summary>
                [Required]
                public string UserID { get; set; }
                /// <summary>
                /// 操作功能
                /// </summary>
                [Required]
                public string FunctionName { get; set; }
                /// <summary>
                /// 操作日期
                /// </summary>
                [Required]
                public string OpDate { get; set; }
                /// <summary>
                /// 操作時間
                /// </summary>
                [Required]
                public string OpTime { get; set; }
                /// <summary>
                /// 操作內容1
                /// </summary>
                public string OptContent1 { get; set; }
                /// <summary>
                /// 操作內容2
                /// </summary>
                public string OptContent2 { get; set; }
                #endregion
            }

            /// <summary>
            /// PACS Job Log
            /// </summary>
            public class JobOptResultLog : JsonDatasetBase
            {
                /// <summary>
                /// 建構
                /// </summary>
                public JobOptResultLog()
                {
                    PatientID = string.Empty;
                    Date = string.Empty;
                    StudyInstanceUID = string.Empty;
                    OptContent = string.Empty;
                    CallingAETitle = string.Empty;
                }

                #region Fields
                /// <summary>
                /// 病歷號碼
                /// </summary>
                public string PatientID { get; set; }
                /// <summary>
                /// 寫入日期
                /// </summary>
                public string Date { get; set; }
                /// <summary>
                /// 檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }
                /// <summary>
                /// 記錄內容
                /// </summary>
                public string OptContent { get; set; }
                /// <summary>
                /// 來源端的AE Title
                /// </summary>
                public string CallingAETitle { get; set; }
                #endregion
            }
        }
    }
}
