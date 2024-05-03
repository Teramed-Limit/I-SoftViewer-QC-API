using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs.PacsServer
{
    #region SvrFileStorageDevice
    /// <summary>
    /// PACS StorageDevice Table
    /// </summary>
    public class SvrFileStorageDevice : JsonDatasetBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrFileStorageDevice()
        {
            StorageDeviceID = string.Empty;
            StoragePath = string.Empty;
            StorageDescription = string.Empty;
            IPAddress = string.Empty;
            UserID = string.Empty;
            UserPassword = string.Empty;
            StorageLevel = string.Empty;
            DicomFilePathRule = string.Empty;
        }

        #region Fields
        /// <summary>
        /// 識別碼
        /// </summary>
        [Required]
        public string StorageDeviceID { get; set; }
        /// <summary>
        /// 儲存檔案根目錄
        /// </summary>
        [Required]
        public string StoragePath { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public string StorageDescription { get; set; }
        /// <summary>
        /// 主機位址
        /// </summary>
        public string IPAddress { get; set; }
        /// <summary>
        /// 登入主機帳號
        /// </summary>
        public string UserID { get; set; }
        /// <summary>
        /// 登入主機密碼
        /// </summary>
        public string UserPassword { get; set; }
        /// <summary>
        /// 儲存類型 10:影像
        /// </summary>
        [Required]
        public string StorageLevel { get; set; }
        /// <summary>
        /// 檔案儲存規則
        /// </summary>
        [Required]
        public string DicomFilePathRule { get; set; }
        #endregion
    }
    #endregion

    #region DeviceHelper
    /// <summary>
    /// FileStorageDevice助手物件
    /// </summary>
    public class DeviceHelper
    {
        /// <summary>
        /// 資料庫欄位Storage Level轉Web
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DbToWebStorageLevel(string value)
        {
            string result;
            if (value == "20")
                result = "Video";
            else
                result = "Image";

            return result;
        }
        /// <summary>
        /// Web Storage Level轉DB欄位
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string WebToDbStorageLevel(string value)
        {
            string result;
            if (value == "Video")
                result = "20";
            else
                result = "10";
            return result;
        }
    }
    #endregion

}
