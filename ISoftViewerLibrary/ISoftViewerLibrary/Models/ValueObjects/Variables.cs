using ISoftViewerLibrary.Model.DicomOperator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.ValueObjects
{
    #region GeneralString
    /// <summary>
    /// 一般字串
    /// </summary>
    public class GeneralString : Value<GeneralString>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="text"></param>
        internal GeneralString(string text) => Value = text;

        #region Fields
        /// <summary>
        /// 資料
        /// </summary>
        public string Value { get; }
        #endregion

        #region Methods
        /// <summary>
        /// 字串轉換
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static GeneralString FromString(string text) => new(text);
        #endregion

        #region operator
        /// <summary>
        /// 隱式轉換
        /// </summary>
        /// <param name="text"></param>
        public static implicit operator string(GeneralString text) => text.Value;
        #endregion
    }
    #endregion

    #region DcmString
    /// <summary>
    /// 包含DICOM Tag的字串
    /// </summary>
    public class DcmString : Value<DcmString>
    {
        public DcmString(ushort g, ushort e, string value, string field)
        {
            TagGroup = g;
            TagElem = e;
            Value = value;
            Field = field;
        }
        public DcmString(string tagNumber, string value, string field)
        {
            new DicomOperatorHelper().ConvertTagStringToUIntGE(tagNumber, out ushort group, out ushort elem);
            TagGroup = group;
            TagElem = elem;
            Value = value;
            Field = field;
        }

        /// <summary>
        /// 建構
        /// </summary>
        internal DcmString()
        {
            TagGroup = 0;
            TagElem = 0;
            Value = "";
            Field = "";
        }

        #region Fields
        /// <summary>
        /// Tag Group
        /// </summary>
        public ushort TagGroup { get; protected set; }
        /// <summary>
        /// Tag Elem
        /// </summary>
        public ushort TagElem { get; protected set; }
        /// <summary>
        /// 資料
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 資料庫欄位
        /// </summary>
        public string Field { get; protected set; }
        #endregion

        #region operator
        /// <summary>
        /// 隱式轉換
        /// 例 : string abc = (DcmString)dcmString;
        /// </summary>
        /// <param name="val"></param>
        public static implicit operator string(DcmString val) => val.Value;
        #endregion
    }
    #endregion

    /// <summary>
    /// QC Mapping表格
    /// </summary>
    public class MappingTagTable
    {
        /// <summary>
        /// 建構
        /// </summary>
        public MappingTagTable()
        {
            Dataset = new ();
        }

        public List<MappingTag> Dataset { get; set; }
    }

    #region MappingTag
    /// <summary>
    /// QC mapping tag
    /// </summary>
    public class MappingTag
    {
        /// <summary>
        /// 建構
        /// </summary>
        public MappingTag()
        {
            Keyword = "";
            FromTag = "";
            ToTag = "";
            Value = "";
        }

        #region Fields
        /// <summary>
        /// Tag Name (辨識用)
        /// </summary>
        public string Keyword { get; set; }
        /// <summary>
        /// 從那一個Tag
        /// </summary>
        public string FromTag { get; set; }
        /// <summary>
        /// 要異動到那個Tag
        /// </summary>
        public string ToTag { get; set; }
        /// <summary>
        /// 固定值
        /// </summary>
        public string Value { get; set; }
        #endregion
    }
    #endregion

    #region MappingTag
    /// <summary>
    /// QC Merge/Split Mapping表格
    /// </summary>
    public class MergeSplitMappingTagTable
    {
        public List<FieldToDcmTagMap> Dataset { get; set; }
    }

    public class FieldToDcmTagMap
    {
        #region Fields
        /// <summary>
        /// 辨識用
        /// </summary>
        public string Keyword { get; set; }
        /// <summary>
        /// 資料表欄位
        /// </summary>
        public string Field { get; set; }
        /// <summary>
        /// Dicom Tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Default Value
        /// </summary>
        public string Default { get; set; }
        #endregion
    }
    #endregion
}
