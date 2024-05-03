using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ISoftViewerLibrary.Models.DTOs
{
    /// <summary>
    /// Represents the QC Auto Mapping Configuration.
    /// </summary>
    public class QCAutoMappingConfig : JsonDatasetBase
    {
        /// <summary>
        /// Gets or sets the station name.
        /// </summary>
        public string StationName { get; set; }

        /// <summary>
        /// Gets or sets the environment setup.
        /// </summary>
        public string EnvSetup { get; set; }

        /// <summary>
        /// Gets or sets the workstation SCP.
        /// </summary>
        public string WkSCP { get; set; }

        /// <summary>
        /// Gets or sets the store SCP.
        /// </summary>
        public string StoreSCP { get; set; }

        /// <summary>
        /// Gets or sets the C-Find request field.
        /// </summary>
        public string CFindReqField { get; set; }

        /// <summary>
        /// Gets or sets the mapping field.
        /// </summary>
        public string MappingField { get; set; }

        /// <summary>
        /// Gets or sets the creation date and time.
        /// </summary>
        public string CreateDateTime { get; set; }

        /// <summary>
        /// Gets or sets the user who created this record.
        /// </summary>
        public string CreateUser { get; set; }

        /// <summary>
        /// Gets or sets the last modified date and time.
        /// </summary>
        public string? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified this record.
        /// </summary>
        public string ModifiedUser { get; set; }
    }

    public class QCAutoMappingConfigDto
    {
        public string StationName { get; set; }
        public EnvSetup EnvSetup  { get; set; }
        public DicomNode WkSCP { get; set; }
        public List<DicomNode> StoreSCP { get; set; }
        public ElementList CFindReqField { get; set; }
        public ElementList MappingField { get; set; }
    }

    /// <summary>
    /// 表示映射配置。
    /// </summary>
    public class EnvSetup
    {
        /// <summary>
        /// 獲取或設置映射等級。
        /// </summary>
        [JsonPropertyName("mappingLevel")]
        public int MappingLevel { get; set; }

        /// <summary>
        /// 獲取或設置映射之間的天數。
        /// </summary>
        [JsonPropertyName("mappingBetweenDay")]
        public int MappingBetweenDay { get; set; }

        /// <summary>
        /// 獲取或設置是否區分大小寫。
        /// </summary>
        [JsonPropertyName("caseSensitive")]
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// 獲取或設置是否啟用。
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// 獲取或設置最後修改日期的間隔。
        /// </summary>
        [JsonPropertyName("lastModifiedDtInterval")]
        public int LastModifiedDtInterval { get; set; }
    }

    public class ElementList
    {
        [JsonPropertyName("elements")]
        public List<Element> Elements { get; set; }
    }

    /// <summary>
    /// 表示工作站 SCP 配置。
    /// </summary>
    public class DicomNode
    {
        [JsonPropertyName("uuid")]
        public string UUID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 獲取或設置遠端 AE Title。
        /// </summary>
        [JsonPropertyName("remoteAETitle")]
        public string RemoteAETitle { get; set; }

        /// <summary>
        /// 獲取或設置本地 AE Title。
        /// </summary>
        [JsonPropertyName("localAETitle")]
        public string LocalAETitle { get; set; }

        /// <summary>
        /// 獲取或設置 IP 地址。
        /// </summary>
        [JsonPropertyName("ip")]
        public string IP { get; set; }

        /// <summary>
        /// 獲取或設置端口。
        /// </summary>
        [JsonPropertyName("port")]
        public int Port { get; set; }

        /// <summary>
        /// 獲取或設置是否使用 UTF-8。
        /// </summary>
        [JsonPropertyName("isUTF8")]
        public bool IsUTF8 { get; set; }
    }

    /// <summary>
    /// 表示 DICOM 元素。
    /// </summary>
    public class Element
    {
        // [JsonPropertyName("uuid")]
        // public string UUID { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// 獲取或設置標籤。
        /// </summary>
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// 獲取或設置值表示（Value Representation）。
        /// </summary>
        [JsonPropertyName("vr")]
        public string VR { get; set; }

        /// <summary>
        /// 獲取或設置規則。
        /// </summary>
        [JsonPropertyName("rule")]
        public string Rule { get; set; }

        /// <summary>
        /// 獲取或設置值。
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; set; }

        /// <summary>
        /// 獲取或設置子元素列表。
        /// </summary>
        [JsonPropertyName("subElements")]
        public List<Element> SubElements { get; set; }
    }
}