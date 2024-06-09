using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{
    #region SvrConfiguration
    /// <summary>
    /// PACS SystemConfiguration Table基底物件
    /// </summary>
    public class SvrConfiguration : JsonDatasetBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrConfiguration()
        {
            Name = default;
            Description = default;
            CStoreBackupFilePath = default;
            CStroeTmpFilesPath = default;
            LogRootPath = default;
            LogLevel = default;
            LogRootPath = default;
            LogRootPath = default;
            LogRootPath = default;
            ErrorImagesPath = default;
            SystemConfigureListenPort = default;
            JobProcessTimerInterval = default;
            DailyTimerInterval = default;
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="fromObj"></param>
        public SvrConfiguration(SvrConfiguration fromObj)
        {
            Name = fromObj.Name;
            Description = fromObj.Description;
            CStoreBackupFilePath = fromObj.CStoreBackupFilePath;
            CStroeTmpFilesPath = fromObj.CStroeTmpFilesPath;
            LogRootPath = fromObj.LogRootPath;
            ErrorImagesPath = fromObj.ErrorImagesPath;
            SystemConfigureListenPort = fromObj.SystemConfigureListenPort;
            JobProcessTimerInterval = fromObj.JobProcessTimerInterval;
            DailyTimerInterval = fromObj.DailyTimerInterval;
        }

        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 收圖備份檔案路徑
        /// </summary>
        [Required]
        public string CStoreBackupFilePath { get; set; }
        /// <summary>
        /// 收圖暫存檔案路徑
        /// </summary>
        [Required]
        public string CStroeTmpFilesPath { get; set; }
        /// <summary>
        /// Log記錄資料夾路徑
        /// </summary>
        [Required]
        public string LogRootPath { get; set; }
        /// <summary>
        /// 目前沒有使用
        /// </summary>
        public int LogLevel { get; set; }
        /// <summary>
        /// 收圖及DICOM服務Log Level
        /// </summary>
        [Required]
        public string PACSMessageWriteToLog { get; set; }
        /// <summary>
        /// Worklist服務Log Level
        /// </summary>
        [Required]
        public string WorklistMessageWriteToLog { get; set; }
        /// <summary>
        /// 排程服務Log Level
        /// </summary>
        [Required]
        public string ScheduleMessageWriteToLog { get; set; }
        /// <summary>
        /// 錯誤影像檔案存放路徑
        /// </summary>
        [Required]
        public string ErrorImagesPath { get; set; }

        /// <summary>
        /// 系統組態監聽埠
        /// </summary>
        [Required]
        public int SystemConfigureListenPort { get; set; }
        /// <summary>
        /// 排程處理間隔時間
        /// </summary>
        [Required]
        public int JobProcessTimerInterval { get; set; }
        /// <summary>
        /// 日常處理計時器間隔時間
        /// </summary>
        [Required]
        public int DailyTimerInterval { get; set; }

    }
    #endregion

    #region ConfigHelper
    /// <summary>
    /// Database欄位與Web資料轉換助手物件
    /// </summary>
    public class ConfigHelper
    {
        /// <summary>
        /// PACS Server Log的型態
        /// </summary>
        public static readonly List<string> ServerMessageTypes = new()
        { 
            "None Message", 
            "Only Error Message", 
            "Main Message", 
            "Normal Message", 
            "Detail Message", 
            "Success Message", 
            "Whole Message" 
        };
        /// <summary>
        /// 資料庫轉換Log Level為Web
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string DbLogLevelToWeb(string level)
        {
            string result;
            if (level == "1")
                result = "Only Error Message";
            else if (level == "2")
                result = "Main Message";
            else if (level == "3")
                result = "Normal Message";
            else if (level == "4")
                result = "Detail Message";
            else if (level == "6")
                result = "Success Message";
            else if (level == "7")
                result = "Whole Message";
            else
                result = "None Message";

            return result;
        }
        /// <summary>
        /// Web Log Level轉換成資料庫欄位
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static string WebLogLevelToDb(string level)
        {
            string result;

            if (level == "Only Error Message")
                result = "1";
            else if (level == "Main Message")
                result = "2";
            else if (level == "Normal Message")
                result = "3";
            else if (level == "Detail Message")
                result = "4";
            else if (level == "Success Message")
                result = "6";
            else if (level == "Whole Message")
                result = "7";
            else
                result = "0";

            return result;
        }
    }
    #endregion

}
