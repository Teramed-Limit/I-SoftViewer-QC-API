using ISoftViewerLibrary.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs.PacsServer
{
    #region SvrDcmProviderBaseValue
    /// <summary>
    /// DicomProvider Table基底物件
    /// </summary>
    public class SvrDcmProviderBaseValue : JsonDatasetBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmProviderBaseValue()
            : base()
        {
            Name = string.Empty;
            AETitle = string.Empty;
            Port = 0;
        }
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmProviderBaseValue(SvrDcmProviderBaseValue dto)
            : base()
        {
            Name = dto.Name;
            AETitle = dto.AETitle;
            Port = dto.Port;
        }

        #region Fields
        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// DICOM Application Entity
        /// </summary>
        [Required]
        public string AETitle { get; set; }
        /// <summary>
        /// 服務埠號
        /// </summary>
        [Required]
        public int Port { get; set; }
        #endregion
    }
    #endregion

    #region DbValue
    /// <summary>
    /// DICOM服務類型, MOD BY JB 增加QRModalSCP
    /// </summary>
    public enum DcmServiceType
    {
        [Description("None")]
        dstNone = 0,
        [Description("Store SCP")]
        dstStoreSCP = 1,
        [Description("Worklist SCP")]
        dstWorklistSCP = 2,
        [Description("QR Modal SCP")]
        dstQRModalSCP = 3,
        [Description("Storage Commitment SCP")]
        dstStorageCommitmentSCP = 4
    };
    /// <summary>
    /// 寫入資料庫資料物件
    /// </summary>
    public class SvrDcmProviderDb : SvrDcmProviderBaseValue
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmProviderDb()
            : base()
        {
            DicomServiceType = 0;
        }
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dto"></param>
        public SvrDcmProviderDb(SvrDcmProviderWeb dto)
            : base(dto)
        {
            DicomServiceType = 0;
            if (NormalHelper.TryGetEnumByDescription(dto.DicomServiceType, true, out DcmServiceType type) == true)
                DicomServiceType = (int)type;
        }

        #region Fields
        /// <summary>
        /// DICOM服務型態
        /// </summary>
        [Required]
        public int DicomServiceType { get; set; }
        
        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }
        #endregion
    }
    #endregion

    #region WebValue
    /// <summary>
    /// DicomProvider Web Page顯示資料
    /// </summary>
    public class SvrDcmProviderWeb : SvrDcmProviderBaseValue
    {
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmProviderWeb()
            : base()
        {
            DicomServiceType = string.Empty;
        }
        /// <summary>
        /// 建構
        /// </summary>
        public SvrDcmProviderWeb(SvrDcmProviderDb dto)
            : base(dto)
        {
            DcmServiceType type = (DcmServiceType)dto.DicomServiceType;
            DicomServiceType = NormalHelper.GetEnumDescription(type);
            CreateDateTime = dto.CreateDateTime;
            CreateUser = dto.CreateUser;
            ModifiedDateTime = dto.ModifiedDateTime;
            ModifiedUser = dto.ModifiedUser;
        }

        #region Fields
        /// <summary>
        /// DICOM服務型態
        /// </summary>
        [Required]
        public string DicomServiceType { get; set; }
        
        public string CreateDateTime { get; set; }

        public string CreateUser { get; set; }

        public string ModifiedDateTime { get; set; }

        public string ModifiedUser { get; set; }
        #endregion
    }
    #endregion
}
