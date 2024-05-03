using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.ValueObjects
{
    /// <summary>
    /// 型別定義類別
    /// </summary>
    public static class Types
    {
        #region enum
        /// <summary>
        /// SCU服務類型
        /// </summary>
        public enum DcmServiceUserType { dsutEcho, dsutStore, dsutFind, dsutMove, dsutWorklist };
        /// <summary>
        /// 搜尋資料庫的類型
        /// </summary>
        public enum DbSearchResultType 
        { 
            dsrSearchStudy = 1, 
            dsrSearchSeries = 2, 
            [Description("SearchImagePathView")]
            dsrSearchImagePath = 3
        };
        #endregion

        #region Methods
        /// <summary>
        /// 取得列舉值說明文字
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumData"></param>
        /// <returns></returns>
        static public string GetEnumDescription<T>(T enumData)
        {            
            FieldInfo fi = enumData.GetType().GetField(enumData.ToString());
            if (fi == null) 
                return "";

            string result = string.Empty;
            object[] attrs = fi.GetCustomAttributes(typeof(DescriptionAttribute), true);
            if (attrs != null && attrs.Length > 0)
                result = ((DescriptionAttribute)attrs[0]).Description;

            return result;
        }
        #endregion

    }
}
