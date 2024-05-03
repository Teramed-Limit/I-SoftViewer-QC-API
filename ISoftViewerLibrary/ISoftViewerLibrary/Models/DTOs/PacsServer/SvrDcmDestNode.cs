using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{
    /// <summary>
    /// DicomDestinationNode資料表欄位
    /// </summary>
    public class SvrDcmDestNode : JsonDatasetBase
    {
        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        public string LogicalName { get; set; }
        /// <summary>
        /// Peer AE Title
        /// </summary>
        [Required]
        public string AETitle { get; set; }
        /// <summary>
        /// Sending SCU AE Title
        /// </summary>
        [Required]
        public string SendingAETitle { get; set; }
        /// <summary>
        /// Peer Host Name
        /// </summary>
        public string HostName { get; set; }
        /// <summary>
        /// Peer IP Address
        /// </summary>
        [Required]
        public string IPAddress { get; set; }
        /// <summary>
        /// Peer Service Port
        /// </summary>
        [Required]
        public int Port { get; set; }
        /// <summary>
        /// 說明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 繞送規則
        /// </summary>
        public string RoutingRulePattern { get; set; }
    }
}
