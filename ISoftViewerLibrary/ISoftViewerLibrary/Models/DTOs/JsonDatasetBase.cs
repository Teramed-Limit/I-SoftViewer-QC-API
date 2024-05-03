using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ISoftViewerLibrary.Models.Interfaces;

namespace ISoftViewerLibrary.Models.DTOs
{
    public class JsonDatasetBase : IJsonDataset
    {
        /// <summary>
        /// 建構
        /// </summary>
        public JsonDatasetBase()
        {
            DataRetrievalFuncs = new Dictionary<string, Func<string>>();
            DataWritingActions = new Dictionary<string, Action<string>>();
        }

        #region Fields
        /// <summary>
        /// JSON欄位取出容器
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, Func<string>> DataRetrievalFuncs { get; protected set; }
        /// <summary>
        /// JSON欄位寫入容器
        /// </summary>        
        [JsonIgnore]
        public IDictionary<string, Action<string>> DataWritingActions { get; protected set; }
        #endregion
    }
}