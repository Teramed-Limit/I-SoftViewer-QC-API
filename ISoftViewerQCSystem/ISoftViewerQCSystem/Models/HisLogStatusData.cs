using System.Collections.Generic;

namespace ISoftViewerQCSystem.Models
{
    /// <summary>
    /// 執行Log時,所記錄的訊息與結果
    /// </summary>
    public class HisLogStatusData
    {
        #region Fields
        /// <summary>
        /// Episode No 本次入院就診號碼
        /// </summary>
        public string EpisodeNo { get; set; }
        /// <summary>
        /// 訊息內容
        /// </summary>
        public List<string> LstMessage { get; set; }
        /// <summary>
        /// 執行結果
        /// </summary>
        public string ExecuteResult { get; set; }
        /// <summary>
        /// 存檔路徑
        /// </summary>
        public string LogFilePath { get; set; }
        #endregion
        /// <summary>
        /// 建構
        /// </summary>
        public HisLogStatusData()
        {
            EpisodeNo = "";
            LstMessage = new List<string>();
            ExecuteResult = "";
            LogFilePath = "";
        }
    }
}