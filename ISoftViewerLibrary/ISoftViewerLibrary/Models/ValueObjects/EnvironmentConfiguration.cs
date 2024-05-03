using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.ValueObjects
{
    #region EnvironmentConfiguration
    /// <summary>
    /// 環境組態
    /// </summary>
    public class EnvironmentConfiguration
    {
        /// <summary>
        /// 建構
        /// </summary>
        public EnvironmentConfiguration()
        {            
        }

        #region Fields
        /// <summary>
        /// 主機名稱
        /// </summary>
        public string ServerName { get; set; } = "";
        /// <summary>
        /// 資料庫名稱
        /// </summary>
        public string DatabaseName { get; set; } = "";
        /// <summary>
        /// 資料庫帳號
        /// </summary>
        public string DBUserID { get; set; } = "";
        /// <summary>
        /// 資料庫密碼
        /// </summary>
        public string DBPassword { get; set; } = "";
        /// <summary>
        /// 虛擬路徑
        /// </summary>
        public string VirtualFilePath { get; set; } = "";
        /// <summary>
        /// DICOM Tag Mapping Table JSON字串
        /// </summary>
        public MappingTagTable DcmTagMappingTable { get; set; } = new ();
        /// <summary>
        /// Merge/Split Mapping tag table
        /// </summary>
        public List<FieldToDcmTagMap> MergeSplitMappingTagTable { get; set; } = new ();
        /// <summary>
        /// Local AE Title
        /// </summary>
        public string CallingAeTitle { get; set; } = "";
        /// <summary>
        /// Server AE Title
        /// </summary>
        public string CalledAeTitle { get; set; } = "";
        /// <summary>
        /// IP
        /// </summary>
        public string DcmSendIP { get; set; } = "";
        /// <summary>
        /// 連接埠
        /// </summary>
        public int DcmSendPort { get; set; } = 0;
        #endregion
    }
    #endregion
}
