using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ISoftViewerLibrary.Models.DTOs.PacsServer
{
    #region DicomNodeBase
    /// <summary>
    /// PACS DicomNode基底物件
    /// </summary>
    public class SvrDcmNodeBaseValue : JsonDatasetBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmNodeBaseValue()
        {
            Name = "";
            AETitle = "";
            IPAddress = "";
            PortNumber = 0;
            RemoteAETitle = "";
            NeedConfirmIPAddress = "";
            Description = "";
            Priority = 0;
            AcceptedTransferSyntaxesCustomize = "";
            TransferSyntaxesCustomize = "";
            WorklistMatchKeys = "";
            WorklistReturnKeys = "";
            ServiceJobTypes = "";
            EnabledAutoRouting = "";
            AuotRoutingDestination = "";
            CreateDateTime = "";
            CreateUser = "";
            ModifiedDateTime = "";
            ModifiedUser = "";
            FilterRulePattern = "";
            Department = "";
        }

        public SvrDcmNodeBaseValue(SvrDcmNodeBaseValue node)
        {
            Name = node.Name;
            AETitle = node.AETitle;
            IPAddress = node.IPAddress;
            PortNumber = node.PortNumber;
            RemoteAETitle = node.RemoteAETitle;
            NeedConfirmIPAddress = node.NeedConfirmIPAddress;
            Description = node.Description;
            Priority = node.Priority;
            AcceptedTransferSyntaxesCustomize = node.AcceptedTransferSyntaxesCustomize;
            TransferSyntaxesCustomize = node.TransferSyntaxesCustomize;
            WorklistMatchKeys = node.WorklistMatchKeys;
            WorklistReturnKeys = node.WorklistReturnKeys;
            ServiceJobTypes = node.ServiceJobTypes;
            EnabledAutoRouting = node.EnabledAutoRouting;
            AuotRoutingDestination = node.AuotRoutingDestination;
            CreateDateTime = node.CreateDateTime;
            CreateUser = node.CreateUser;
            ModifiedDateTime = node.ModifiedDateTime;
            ModifiedUser = node.ModifiedUser;
            FilterRulePattern = node.FilterRulePattern;
            Department = node.Department;
        }
        #region Fields
        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// Local AE
        /// </summary>
        [Required]
        public string AETitle { get; set; }
        /// <summary>
        /// Local IP位址
        /// </summary>
        public string IPAddress { get; set; }
        /// <summary>
        /// Server Port
        /// </summary>
        public int PortNumber { get; set; }
        /// <summary>
        /// Server AE Title
        /// </summary>
        [Required]
        public string RemoteAETitle { get; set; }
        /// <summary>
        /// 是否需要確認IP位址
        /// </summary>
        public string NeedConfirmIPAddress { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public int Priority { get; set; }
        /// <summary>
        /// 是否允許客製化檔案格式
        /// </summary>
        public string AcceptedTransferSyntaxesCustomize { get; set; }
        /// <summary>
        /// 客製化檔案格式
        /// </summary>
        public string TransferSyntaxesCustomize { get; set; }
        /// <summary>
        /// Worklist 查詢條件
        /// </summary>
        public string WorklistMatchKeys { get; set; }
        /// <summary>
        /// Worklist 查詢結果
        /// </summary>
        public string WorklistReturnKeys { get; set; }
        /// <summary>
        /// 排程類型
        /// </summary>
        public string ServiceJobTypes { get; set; }
        /// <summary>
        /// 是否啟用DICOM檔案繞送功能
        /// </summary>
        public string EnabledAutoRouting { get; set; }
        /// <summary>
        /// 繞送目的地
        /// </summary>
        public string AuotRoutingDestination { get; set; }
        /// <summary>
        /// 建立日期時間
        /// </summary>
        public string CreateDateTime { get; set; }
        /// <summary>
        /// 建立使用者帳號
        /// </summary>
        public string CreateUser { get; set; }
        /// <summary>
        /// 修改日期時間
        /// </summary>
        public string ModifiedDateTime { get; set; }
        /// <summary>
        /// 修改使用者帳號
        /// </summary>
        public string ModifiedUser { get; set; }
        /// <summary>
        /// 過濾規則樣式
        /// </summary>
        public string FilterRulePattern { get; set; }
        /// <summary>
        /// 部門
        /// </summary>
        public string Department { get; set; }
        #endregion
    }
    #endregion

    #region SvrDcmNodeDb
    /// <summary>
    /// DicomNode Database
    /// </summary>
    public class SvrDcmNodeDb : SvrDcmNodeBaseValue
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmNodeDb()
            : base()
        {
            ImageCompression = 0;
            CompressQuality = 0;
            WorklistQueryPattern = 0;
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="nodeWeb"></param>
        public SvrDcmNodeDb(SvrDcmNodeWeb nodeWeb)
            : base(nodeWeb)
        {
            // ServiceJobTypes = NodeHelper.JobTypeToDbField(nodeWeb.ServiceJobTypes);
            ServiceJobTypes = nodeWeb.ServiceJobTypes;
            ImageCompression = NodeHelper.WebForNeedCompressToDb(nodeWeb.ImageCompression);
            CompressQuality = NodeHelper.WebForCompressQualityToDbField(nodeWeb.CompressQuality);
            WorklistQueryPattern = NodeHelper.WebForWlmQryPatternToDbField(nodeWeb.WorklistQueryPattern);
        }

        #region Fields
        /// <summary>
        /// 是否支援影像壓縮
        /// </summary>
        public int ImageCompression { get; set; }
        /// <summary>
        /// 壓縮品質
        /// </summary>
        public int CompressQuality { get; set; }
        /// <summary>
        /// Worklist查詢樣式
        /// </summary>
        public int WorklistQueryPattern { get; set; }
        #endregion
    }
    #endregion

    #region SvrDcmNodeWeb
    /// <summary>
    /// DicomNode Web
    /// </summary>
    public class SvrDcmNodeWeb : SvrDcmNodeBaseValue
    {
        public SvrDcmNodeWeb()
        {
            ImageCompression = "";
            CompressQuality = "";
            WorklistQueryPattern = "";
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="nodeDb"></param>
        public SvrDcmNodeWeb(SvrDcmNodeDb nodeDb)
            : base(nodeDb)
        {
            // ServiceJobTypes = NodeHelper.DbFiledToJobType(nodeDb.ServiceJobTypes);
            ServiceJobTypes = nodeDb.ServiceJobTypes;
            ImageCompression = NodeHelper.DbForNeedCompressToWeb(nodeDb.ImageCompression);
            CompressQuality = NodeHelper.DbForCompressQualityToWeb(nodeDb.CompressQuality);
            WorklistQueryPattern = NodeHelper.DbForWlmQryPatternToWeb(nodeDb.WorklistQueryPattern);
        }

        #region Fields
        /// <summary>
        /// 是否支援影像壓縮
        /// </summary>
        public string ImageCompression { get; set; }
        /// <summary>
        /// 壓縮品質
        /// </summary>
        public string CompressQuality { get; set; }
        /// <summary>
        /// Worklist查詢樣式
        /// </summary>
        public string WorklistQueryPattern { get; set; }
        #endregion
    }
    #endregion

    #region NodeHelper
    /// <summary>
    /// DicomNode DB/Web 轉換助手物件
    /// </summary>
    public class NodeHelper
    {
        #region Fields
        /// <summary>
        /// 排程類型
        /// </summary>
        public static readonly List<string> ScheduldJobTypes = new()
        {
            "CStoreFileSave",
            "CStoreFileSave -> DicomToThumbnail",
            "CStoreFileSave -> DicomToThumbnail -> RoutingDicom",
            "RoutingDicom",
            "RoutingDicomAfterDeleteFile",
            "CUHKCustomzedPID -> RoutingDicomAfterDeleteFile"
        };
        #endregion

        #region Methods
        /// <summary>
        /// 排程轉成資料庫存放資料
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string JobTypeToDbField(string value)
        {
            string result = string.Empty;
            if (value == "None Job")
                result = "0";
            if (value == "CStoreFileSave")
                result = "1";
            if (value == "CStoreFileSave -> DicomToThumbnail")
                result = "1|0";
            if (value == "CStoreFileSave -> DicomToThumbnail -> RoutingDicom")
                result = "3|1|0";
            if (value == "RoutingDicom")
                result = "3";
            if (value == "RoutingDicomAfterDeleteFile")
                result = "4";
            if (value == "CUHKCustomzedPID -> RoutingDicomAfterDeleteFile")
                result = "4|7";
            if (value == "CStoreFileSave -> CStoreFVideoSave -> DicomToThumbnail -> RoutingDicom")
                result = "11|10";

            return result;
        }
        /// <summary>
        /// 資料庫資料轉排程字串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DbFiledToJobType(string value)
        {
            string result = string.Empty;
            if (value == "0")
                result = "None Job";
            if (value == "1")
                result = "CStoreFileSave";
            if (value == "1|0")
                result = "CStoreFileSave -> DicomToThumbnail";
            if (value == "3|1|0")
                result = "CStoreFileSave -> DicomToThumbnail -> RoutingDicom";
            if (value == "3")
                result = "RoutingDicom";
            if (value == "4")
                result = "RoutingDicomAfterDeleteFile";
            if (value == "4|7")
                result = "CUHKCustomzedPID -> RoutingDicomAfterDeleteFile";

            return result;
        }
        /// <summary>
        /// ImageCompression Web to Db Field
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int WebForNeedCompressToDb(string value)
        {
            int result = 1;
            if (value == "False")
                result = 0;

            return result;
        }
        /// <summary>
        /// ImageCompression Db Field to Web
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DbForNeedCompressToWeb(int value)
        {
            string result = "True";
            if (value == 0)
                result = "False";
            return result;
        }
        /// <summary>
        /// Compress Quality Db Field to Web
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DbForCompressQualityToWeb(int value)
        {
            string result;
            if (value >= 90)
                result = "HIGH";
            else if (value >= 80 && value < 90)
                result = "MIDDLE";
            else
                result = "LOW";
            return result;
        }
        /// <summary>
        /// Compress Quality Web To Db Field
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int WebForCompressQualityToDbField(string value)
        {
            int result;
            if (value == "HIGH")
                result = 90;
            else if (value == "MIDDLE")
                result = 80;
            else
                result = 60;
            return result;
        }
        /// <summary>
        /// WorklistQueryPattern Db to Web
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DbForWlmQryPatternToWeb(int value)
        {
            string result = "Database";
            if (value == 1)
                result = "HIS SDK";
            return result;
        }
        /// <summary>
        /// WorklistQueryPattern Web to Db
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int WebForWlmQryPatternToDbField(string value)
        {
            int result = 0;
            if (value == "HIS SDK")
                result = 1;
            return result;
        }
        #endregion
    }
    #endregion

}