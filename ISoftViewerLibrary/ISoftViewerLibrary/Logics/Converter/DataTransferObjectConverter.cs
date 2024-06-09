using Dicom;
using ISoftViewerLibrary.Logic.Interfaces;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace ISoftViewerLibrary.Logic.Converter
{
    #region JsonData2TableConverter

    /// <summary>
    /// JSON資料轉ElementAbstract
    /// </summary>
    public class JsonData2TableConverter : IDataConvertAdapter<IJsonDataset, IElementInterface>, IDisposable
    {
        /// <summary>
        /// 建構
        /// </summary>
        public JsonData2TableConverter()
        {
            Message = "";
            Result = OpResult.OpSuccess;
        }

        #region Fields

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// 從fromData轉換到指定的物件型態
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public virtual bool Convert(IJsonDataset fromData, IElementInterface convertTo)
        {
            try
            {
                TableElementHelper tbElementHelper = new TableElementHelper();
                //由JSON的屬性名稱和類型去轉資料到ElementAbstract欄位中
                Type jsonType = fromData.GetType();
                foreach (PropertyInfo property in jsonType.GetProperties())
                {
                    //string type = property.GetType().Name;
                    string name = property.Name;

                    ICommonFieldProperty cmField = TableElementHelper.FindField(convertTo, name);
                    if (cmField == null)
                        continue;

                    if (property.PropertyType == typeof(Byte[]))
                    {
                        //MOD BY JB 20210427 如果沒有資料,產生一個Byte,避免資料寫入或更新問題
                        //MOD BY JB 20211025 改為强制資料判斷
                        if (fromData.GetType().GetProperty(name).GetValue(fromData, null) is byte[] values)
                            cmField.UpdateDbFieldValues("", "", values);
                        else
                            cmField.UpdateDbFieldValues("", "", new Byte[1]);
                        //if (fromData.GetType().GetProperty(name).GetValue(fromData, null) is byte[] values)
                        //    cmField.BinaryValue = values;
                        //else
                        //    cmField.BinaryValue = new Byte[1];
                    }
                    else
                    {
                        //Int32, String
                        string value = fromData.GetType().GetProperty(name)?.GetValue(fromData, null)?.ToString();
                        //MOD BY JB 20211025 改為强制資料判斷
                        cmField.UpdateDbFieldValues(value, "", null);
                        //cmField.Value = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region DataAndTypeConverterTool<T1, T2>

    /// <summary>
    /// Table轉JSON資料轉換器
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    public class DataAndTypeConverterTool<T1, T2> : IDisposable
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DataAndTypeConverterTool()
        {
        }

        #region Methods

        /// <summary>
        /// 資料及型態轉換
        /// </summary>
        /// <param name="cmField"></param>
        /// <param name="convertTo"></param>
        public void DataAndTypeConverter(ICommonFieldProperty cmField, string name, IJsonDataset convertTo)
        {
            Type jsonType = convertTo.GetType();
            if (cmField.Type == FieldType.ftBinary)
            {
                jsonType.GetProperty(name).SetValue(convertTo, cmField.BinaryValue, null);
            }
            else if (cmField.Type == FieldType.ftInt)
            {
                if (int.TryParse(cmField.Value, out int tmpInt32) == true)
                    jsonType.GetProperty(name).SetValue(convertTo, tmpInt32, null);
                else
                    jsonType.GetProperty(name).SetValue(convertTo, 0, null);
            }
            else if (cmField.Type == FieldType.ftBoolean)
            {
                if (bool.TryParse(cmField.Value, out bool tmpBool) == true)
                    jsonType.GetProperty(name).SetValue(convertTo, tmpBool, null);
                else
                    jsonType.GetProperty(name).SetValue(convertTo, false, null);
            }
            else
            {
                jsonType.GetProperty(name).SetValue(convertTo, cmField.Value, null);
            }
        }

        /// <summary>
        /// 垃圾回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region TableData2JSONConverter

    /// <summary>
    /// ElementAbstract資料轉JSON
    /// </summary>
    public class TableData2JSONConverter : IDataConvertAdapter<IElementInterface, IJsonDataset>
    {
        /// <summary>
        /// 建構
        /// </summary>
        public TableData2JSONConverter()
        {
            Message = "";
            Result = OpResult.OpSuccess;
        }

        #region Fields

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// 從fromData轉換到指定的物件型態
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public virtual bool Convert(IElementInterface fromData, IJsonDataset convertTo)
        {
            try
            {
                TableElementHelper tbElementHelper = new TableElementHelper();
                //這個部份一份,由JSON去找Table資料,在把Table裡的資料寫入到JSON之中,因為Table拿來做搜尋比較方便,Type.Properties比較不好操作
                Type jsonType = convertTo.GetType();
                using (DataAndTypeConverterTool<IElementInterface, IJsonDataset> converterTool =
                       new DataAndTypeConverterTool<IElementInterface, IJsonDataset>())
                {
                    foreach (PropertyInfo property in jsonType.GetProperties( /*BindingFlags.Public*/))
                    {
                        string name = property.Name;
                        //Mark Oscar 20210914 用不到
                        //string type = property.GetType().Name;
                        ICommonFieldProperty cmField = TableElementHelper.FindField(fromData, name);
                        if (cmField == null)
                            continue;
                        //做資料轉換
                        converterTool.DataAndTypeConverter(cmField, name, convertTo);
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        #endregion
    }

    #endregion

    #region MultiTableData2JSONConverter

    /// <summary>
    /// ElementAbstract資料轉JSON
    /// </summary>
    public abstract class
        MultiTableData2JsonConverter<T> : IDataConvertAdapter<Dictionary<string, IElementInterface>, IJsonDataset>
    {
        /// <summary>
        ///     建構
        /// </summary>
        protected MultiTableData2JsonConverter()
        {
            Message = "";
            Result = OpResult.OpSuccess;
        }

        #region Fields

        /// <summary>
        ///     訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        ///     結果
        /// </summary>
        public OpResult Result { get; protected set; }

        #endregion

        #region Methods

        public abstract void CustomDtoConvert(T jsonDataSet, Dictionary<string, IElementInterface> elementInterfaces);

        public virtual bool Convert(Dictionary<string, IElementInterface> fromData, IJsonDataset convertTo)
        {
            try
            {
                if (!(convertTo is T jsonDataSet))
                    return false;

                CustomDtoConvert(jsonDataSet, fromData);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        protected List<T1> ConvertTableElementToJsonData<T1>(IElementInterface tableElement)
            where T1 : IJsonDataset, new()
        {
            List<T1> container = new List<T1>();
            using (DataAndTypeConverterTool<IElementInterface, IJsonDataset> converterTool =
                   new DataAndTypeConverterTool<IElementInterface, IJsonDataset>())
            {
                tableElement.DBDatasets.ForEach(dataset =>
                {
                    T1 convertTo = new T1();
                    Type jsonType = convertTo.GetType();
                    foreach (PropertyInfo property in jsonType.GetProperties())
                    {
                        string name = property.Name;
                        ICommonFieldProperty cmField = dataset.Find(field => field.FieldName == name);
                        if (cmField == null)
                            continue;
                        //做資料轉換
                        converterTool.DataAndTypeConverter(cmField, name, convertTo);
                    }

                    container.Add(convertTo);
                });
            }

            return container;
        }

        #endregion
    }

    #endregion

    #region CommonFieldProperties2JSONConverter

    /// <summary>
    /// 多欄位轉JSON轉換器
    /// </summary>
    public class CommonFieldProperties2JSONConverter : IDataConvertAdapter<List<ICommonFieldProperty>, IJsonDataset>
    {
        /// <summary>
        /// 建構
        /// </summary>
        public CommonFieldProperties2JSONConverter()
        {
        }

        #region Fields

        /// <summary>
        /// 處理訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// 處理結果
        /// </summary>
        public OpResult Result { get; protected set; }

        #endregion

        #region Methdos

        /// <summary>
        /// 轉換欄位到JSON格式
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public bool Convert(List<ICommonFieldProperty> fromData, IJsonDataset convertTo)
        {
            try
            {
                TableElementHelper tbElementHelper = new TableElementHelper();
                //這個部份一份,由JSON去找Table資料,在把Table裡的資料寫入到JSON之中,因為Table拿來做搜尋比較方便,Type.Properties比較不好操作
                Type jsonType = convertTo.GetType();
                using (DataAndTypeConverterTool<IElementInterface, IJsonDataset> converterTool =
                       new DataAndTypeConverterTool<IElementInterface, IJsonDataset>())
                {
                    foreach (PropertyInfo property in jsonType.GetProperties( /*BindingFlags.Public*/))
                    {
                        string name = property.Name;
                        ICommonFieldProperty cmField = fromData.Find(commonField =>
                            commonField.FieldName == name || commonField.AliasFieldName == name);
                        if (cmField == null)
                            continue;
                        //做資料轉換
                        converterTool.DataAndTypeConverter(cmField, name, convertTo);
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        #endregion
    }

    #endregion

    #region PairDatas2TableConverter

    /// <summary>
    /// 成對參數轉成Table轉換器
    /// </summary>
    public class PairDatas2TableConverter : IDataConvertAdapter<List<PairDatas>, IElementInterface>
    {
        /// <summary>
        /// 建構
        /// </summary>
        public PairDatas2TableConverter()
        {
        }

        #region Fields

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; protected set; }

        #endregion

        #region Methods

        /// <summary>
        /// 將成對查詢資料轉成表格查詢物件
        /// </summary>
        /// <param name="fromData"></param>
        /// <param name="convertTo"></param>
        /// <returns></returns>
        public bool Convert(List<PairDatas> fromData, IElementInterface convertTo)
        {
            try
            {
                TableElementHelper tbElementHelper = new TableElementHelper();
                foreach (var data in fromData)
                {
                    ICommonFieldProperty cmField = TableElementHelper.FindField(convertTo, data.Name);
                    ////MOD BY JB 20211025 改為强制資料判斷
                    if (cmField != null)
                        cmField.UpdateDbFieldValues(data.Value, "", null);
                    //cmField.Value = data.Value;                    
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        #endregion
    }

    #endregion

    #region DcmDatasetConvert2DcmTagData

    /// <summary>
    /// DcmDatasetConvert轉DcmTagData轉換器
    /// </summary>
    public class DcmDatasetConvert2DcmTagData
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dataset"></param>
        public DcmDatasetConvert2DcmTagData()
        {
            IsUtf8Encoding = false;
        }

        #region Fields

        /// <summary>
        /// 是否為UTF8編碼
        /// </summary>
        protected bool IsUtf8Encoding;

        #endregion

        #region Methods

        /// <summary>
        /// 轉換DicomDataset成DcmTagData列表
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public List<DataCorrection.V1.DcmTagData> ConvertToDcmTagData(DicomDataset dataset)
        {
            //取得資料時,必需先確定要使用何種文字編碼                
            if (dataset.TryGetString(DicomTag.SpecificCharacterSet, out string elementVal) == true)
            {
                if (elementVal.Trim() == "ISO_IR 192" || elementVal.Trim() == "ISO_IR_192")
                    IsUtf8Encoding = true;
                else
                    IsUtf8Encoding = false;
            }

            List<DataCorrection.V1.DcmTagData> dcmTagDatas = new();
            foreach (var dcmItem in dataset)
            {
                //區分巢狀和非巢狀
                if (dcmItem.ValueRepresentation == DicomVR.SQ)
                    HandleSequenceItem(dcmItem, ref dcmTagDatas);
                else
                    HandleNonSequenceItem(dcmItem, ref dcmTagDatas);
            }

            return dcmTagDatas;
        }

        /// <summary>
        /// 根據資料集填充 DICOM 標籤資料列表
        /// </summary>
        /// <param name="dataset">fo-dicom 資料集</param>
        /// <returns>填充的 DICOM 標籤資料列表</returns>
        public List<DataCorrection.V1.DcmTagData> PopulateFromDataset(DicomDataset dataset)
        {
            if (dataset.TryGetString(DicomTag.SpecificCharacterSet, out string elementVal) == true)
            {
                if (elementVal.Trim() == "ISO_IR 192" || elementVal.Trim() == "ISO_IR_192")
                    IsUtf8Encoding = true;
            }

            var dcmTagDatas = new List<DataCorrection.V1.DcmTagData>();

            // 遞迴填充資料
            foreach (var dcmItem in dataset)
            {
                DicomTag dTag = dcmItem.Tag;
                DicomVR dVR = dcmItem.ValueRepresentation;
                string value = "";

                DicomOperatorHelper dcmHelper = new();
                DataCorrection.V1.DcmTagData tagData = null;
                if (dcmHelper.GetDicomValueToStringFromDicomItem(dcmItem, dVR, ref value, IsUtf8Encoding))
                    tagData = MakeDcmTagData(dTag, value);

                // 如果有子資料集，遞迴處理
                if (dcmItem.ValueRepresentation == DicomVR.SQ)
                {
                    if (dcmItem is not DicomSequence dSequence)
                        continue;

                    foreach (var subDataset in dSequence)
                    {
                        var subTagData = new DataCorrection.V1.DcmTagData();
                        subTagData.SeqDcmTagData = PopulateFromDataset(subDataset);
                        if (tagData.SeqDcmTagData.Any())
                            tagData.SeqDcmTagData.AddRange(subTagData.SeqDcmTagData);
                        else
                            tagData.SeqDcmTagData = subTagData.SeqDcmTagData;
                    }
                }

                dcmTagDatas.Add(tagData);
            }

            return dcmTagDatas;
        }

        /// <summary>
        /// 產生DcmTagData
        /// </summary>
        /// <param name="dcmTag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected DataCorrection.V1.DcmTagData MakeDcmTagData(DicomTag dcmTag, string value)
        {
            DataCorrection.V1.DcmTagData dcmTagData = new()
            {
                Group = dcmTag.Group,
                Elem = dcmTag.Element,
                Value = value,
                Name = dcmTag.DictionaryEntry.Name,
                Keyword =
                    char.ToLowerInvariant(dcmTag.DictionaryEntry.Keyword[0]) + dcmTag.DictionaryEntry.Keyword[1..],
            };
            return dcmTagData;
        }

        /// <summary>
        /// None Sequence Item
        /// </summary>
        /// <param name="dcmItem"></param>
        /// <param name="itemValues"></param>
        protected void HandleNonSequenceItem(DicomItem dcmItem, ref List<DataCorrection.V1.DcmTagData> itemValues)
        {
            DicomTag dTag = dcmItem.Tag;
            DicomVR dVR = dcmItem.ValueRepresentation;
            string value = "";

            DicomOperatorHelper dcmHelper = new();
            if (dcmHelper.GetDicomValueToStringFromDicomItem(dcmItem, dVR, ref value, IsUtf8Encoding) == true)
                itemValues.Add(MakeDcmTagData(dTag, value));
        }

        /// <summary>
        /// Sequence Item
        /// </summary>
        /// <param name="dcmItem"></param>
        /// <param name="itemValues"></param>
        protected void HandleSequenceItem(DicomItem dcmItem, ref List<DataCorrection.V1.DcmTagData> itemValues)
        {
            if (dcmItem is not DicomSequence dSequence)
                return;
            MakeDcmTagData(dSequence.Tag, dSequence.Tag.ToString());
            foreach (DicomDataset item in dSequence.Items)
            {
                foreach (var subItem in item)
                {
                    //區分巢狀和非巢狀
                    if (subItem.ValueRepresentation == DicomVR.SQ)
                        HandleSequenceItem(subItem, ref itemValues);
                    else
                        HandleNonSequenceItem(subItem, ref itemValues);
                }
            }
        }

        #endregion
    }

    #endregion
}