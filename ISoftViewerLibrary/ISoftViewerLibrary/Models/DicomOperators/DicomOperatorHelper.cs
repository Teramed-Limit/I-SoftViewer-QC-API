using Dicom;
using Dicom.IO;
using Dicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using Newtonsoft.Json;

namespace ISoftViewerLibrary.Model.DicomOperator
{
    public class DicomOperatorHelper
    {
        # region Constructor

        public DicomOperatorHelper()
        {
            // 看有沒有appsetting.json，從裡面取得設定值，直接找檔案
            var settingsFile = AppDomain.CurrentDomain.BaseDirectory + "appsettings.json";
            if (!File.Exists(settingsFile)) return;

            // 直接解析json檔案
            using (StreamReader r = new StreamReader(settingsFile))
            {
                string json = r.ReadToEnd();
                var settings = JsonConvert.DeserializeObject<EnvironmentConfiguration>(json);
                AnsiEncoding = Encoding.GetEncoding(settings.AnsiEncoding);
            }
        }

        public Encoding AnsiEncoding { get; set; } = Encoding.GetEncoding("big5");

        # endregion

        #region GetDicomValueToStringWithGroupAndElem

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="group"></param>
        /// <param name="elem"></param>
        /// <returns></returns>
        public string GetDicomValueToStringWithGroupAndElem(DicomDataset dataset, ushort group, ushort elem,
            bool isUtf8)
        {
            DicomTag dcmTag = new DicomTag(group, elem);

            if (dataset.Contains(dcmTag) == false)
                return "";
            //判斷是那一種型態
            var entry = DicomDictionary.Default[dcmTag];
            DicomVR vr = entry.ValueRepresentations.First();

            string value = GetDicomValueToString(dataset, dcmTag, vr, isUtf8).Trim();
            value = value.Replace('\0', ' ').Trim();

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="tag"></param>
        /// <param name="isUtf8"></param>
        /// <returns></returns>
        public string GetDicomValueToStringWithGroupAndElem(DicomDataset dataset, string tag, bool isUtf8)
        {
            // 代表是Sequence的Tag
            if (tag.Contains("|"))
            {
                string[] tags = tag.Split('|');
                return GetDicomValueRecursive(dataset, tags, 0, isUtf8);
            }
            else
            {
                var tagArray = tag.Split(',');
                ushort group = Convert.ToUInt16(tagArray[0], 16);
                ushort elem = Convert.ToUInt16(tagArray[1], 16);
                DicomTag dcmTag = new DicomTag(group, elem);

                if (dataset.Contains(dcmTag) == false)
                    return "";
                //判斷是那一種型態
                var entry = DicomDictionary.Default[dcmTag];
                DicomVR vr = entry.ValueRepresentations.First();

                string value = GetDicomValueToString(dataset, dcmTag, vr, isUtf8).Trim();
                value = value.Replace('\0', ' ').Trim();

                return value;
            }
        }

        /// <summary>
        /// 取得指定 DcmTagData 的 value
        /// </summary>
        /// <param name="dicomTags">DICOM Tag 資料列表</param>
        /// <param name="tagPath">Tag 路徑</param>
        /// <returns>對應的 DcmTagData 的 value</returns>
        public string GetDicomValueToStringWithDcmTagData(List<DataCorrection.V1.DcmTagData> dicomTags, string tagPath)
        {
            var tags = tagPath.Split('|');
            return GetDcmTagDataValueRecursive(dicomTags, tags, 0);
        }
        
        public DicomTag ConvertStringToDicomTag(string tagString)
        {
            // 檢查輸入的字串格式是否正確
            if (tagString.Length != 9 || tagString[4] != ',')
            {
                throw new ArgumentException("String format is incorrect, should be 'XXXX,XXXX' format");
            }

            // 分割字串，取得群組和元素部分
            string[] parts = tagString.Split(',');
            string groupPart = parts[0];
            string elementPart = parts[1];

            // 將群組和元素部分轉換為整數
            ushort group = ushort.Parse(groupPart, System.Globalization.NumberStyles.HexNumber);
            ushort element = ushort.Parse(elementPart, System.Globalization.NumberStyles.HexNumber);

            // 建立並返回DicomTag物件
            return new DicomTag(group, element);
        }
        

        /// <summary>
        /// 遞迴取得 DcmTagData 的 value
        /// </summary>
        /// <param name="dicomTags">DICOM Tag 資料列表</param>
        /// <param name="tags">Tag 路徑陣列</param>
        /// <param name="index">目前處理的 Tag 索引</param>
        /// <returns>對應的 DcmTagData 的 value</returns>
        private static string GetDcmTagDataValueRecursive(List<DataCorrection.V1.DcmTagData> dicomTags, string[] tags,
            int index)
        {
            if (index >= tags.Length)
            {
                return string.Empty;
            }

            var tagParts = tags[index].Split(',');
            if (tagParts.Length != 2)
            {
                throw new ArgumentException("Invalid tag format, should be 'gggg,eeee' but got '" + tags[index] + "'");
            }

            var group = Convert.ToUInt32(tagParts[0], 16);
            var elem = Convert.ToUInt32(tagParts[1], 16);

            var currentTag = dicomTags.Find(tag => tag.Group == group && tag.Elem == elem);
            if (currentTag == null)
            {
                return string.Empty;
            }

            if (index == tags.Length - 1)
            {
                return currentTag.Value;
            }

            return GetDcmTagDataValueRecursive(currentTag.SeqDcmTagData, tags, index + 1);
        }

        private static string GetDicomValueRecursive(DicomDataset dataset, string[] tags, int index, bool isUtf8)
        {
            if (index >= tags.Length) return null;

            string[] parts = tags[index].Split(',');
            ushort group = Convert.ToUInt16(parts[0], 16);
            ushort elem = Convert.ToUInt16(parts[1], 16);
            DicomTag dicomTag = new DicomTag(group, elem);

            var entry = DicomDictionary.Default[dicomTag];
            DicomVR vr = entry.ValueRepresentations.First();
            var isSequence = vr == DicomVR.SQ;

            if (isSequence)
            {
                // 如果是序列，遍歷序列中的每一個項目
                if (dataset.TryGetSequence(dicomTag, out DicomSequence sequence))
                {
                    foreach (var item in sequence.Items)
                    {
                        string value = GetDicomValueRecursive(item, tags, index + 1, isUtf8);
                        if (value != null) return value;
                    }
                }
            }
            else
            {
                // 如果不是序列，返回對應的值
                return new DicomOperatorHelper()
                    .GetDicomValueToStringWithGroupAndElem(dataset, dicomTag.Group, dicomTag.Element, isUtf8);
            }

            return null; // 如果最終未找到目標 tag，返回空
        }

        #endregion

        #region GetDicomValueToString

        /// <summary>
        /// 依照VR的型態去取得資料
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="tag"></param>
        /// <param name="vr"></param>
        /// <returns></returns>
        public string GetDicomValueToString(DicomDataset dataset, DicomTag tag, DicomVR vr, bool isUTF8Encoding)
        {
#pragma warning disable CS0618 // 類型或成員已經過時
            string result = default(string);

            //string
            if (vr == DicomVR.AE || vr == DicomVR.AS || vr == DicomVR.AT || vr == DicomVR.CS || vr == DicomVR.DA ||
                vr == DicomVR.DS || vr == DicomVR.DT || vr == DicomVR.IS || vr == DicomVR.LO || vr == DicomVR.LT ||
                vr == DicomVR.PN || vr == DicomVR.SH || vr == DicomVR.ST || vr == DicomVR.TM || vr == DicomVR.UI ||
                vr == DicomVR.UT)
            {
                //由於會有中文問題,所以需要判斷是否需要進行編碼
                DicomElement element = dataset.GetDicomItem<DicomElement>(tag);
                if (element != null)
                {
                    string value = "";
                    if (isUTF8Encoding)
                    {
                        var utf8Encoding = Encoding.GetEncoding("utf-8");
                        value = utf8Encoding.GetString(element.Buffer.Data);
                    }

                    else
                    {
                        value = AnsiEncoding.GetString(element.Buffer.Data);
                    }

                    result = value;
                }
            }

            //dobule, float
            if (vr == DicomVR.FD || vr == DicomVR.FL || vr == DicomVR.OF || vr == DicomVR.OD || vr == DicomVR.OL)
            {
                result = Convert.ToString(value: dataset.Get(tag, 0.0));
            }

            //int
            if (vr == DicomVR.SL)
            {
                result = Convert.ToString(value: dataset.Get(tag, 1));
            }

            //uint
            if (vr == DicomVR.UL)
            {
                result = Convert.ToString(dataset.Get<uint>(tag, 1));
            }

            //ushort
            if (vr == DicomVR.OW || vr == DicomVR.US)
            {
                result = Convert.ToString(dataset.Get<ushort>(tag, 1));
            }

            //short
            if (vr == DicomVR.SS)
            {
                result = Convert.ToString(dataset.Get<short>(tag, 1));
            }
#pragma warning restore CS0618 // 類型或成員已經過時
            //其它,暫不處理              
            return result.Replace('\0', ' ').Trim();
            ;
        }

        #endregion

        #region WriteDicomValueInDataset

        public void WriteDicomValueInDataset(DicomDataset dataset, DicomTag tag, string value, bool isUtf8)
        {
            var entry = DicomDictionary.Default[tag];
            DicomVR vr = entry.ValueRepresentations.First();

#pragma warning disable CS0618 // 類型或成員已經過時            
            //string
            if (vr == DicomVR.AE || vr == DicomVR.AS || vr == DicomVR.AT || vr == DicomVR.CS || vr == DicomVR.DA ||
                vr == DicomVR.DS || vr == DicomVR.DT || vr == DicomVR.IS ||
                vr == DicomVR.TM || vr == DicomVR.UI)
            {
                dataset.AddOrUpdate<string>(tag, value);
            }

            //string,可能支援中文的Element要另外處理
            if (vr == DicomVR.LO || vr == DicomVR.LT || vr == DicomVR.PN || vr == DicomVR.ST || vr == DicomVR.UC ||
                vr == DicomVR.UL || vr == DicomVR.UT || vr == DicomVR.SH || vr == DicomVR.UR)
            {
                DicomItem[] items = new DicomItem[1];

                // 會有中文問題所以預設轉成BYTE去寫
                IByteBuffer buffer;
                Encoding characterSetCode;
                if (isUtf8)
                {
                    characterSetCode = Encoding.GetEncoding("utf-8");
                    buffer = ByteConverter.ToByteBuffer(value ?? String.Empty, Encoding.GetEncoding("utf-8"), 32);
                }
                else
                {
                    characterSetCode = AnsiEncoding;
                    buffer = ByteConverter.ToByteBuffer(value ?? String.Empty, characterSetCode, 32);
                }

                if (vr == DicomVR.LO)
                    items[0] = new DicomLongString(tag, characterSetCode, buffer);
                else if (vr == DicomVR.LT)
                    items[0] = new DicomLongText(tag, characterSetCode, buffer);
                else if (vr == DicomVR.PN)
                    items[0] = new DicomPersonName(tag, characterSetCode, buffer);
                else if (vr == DicomVR.SH)
                    items[0] = new DicomShortString(tag, characterSetCode, buffer);
                else if (vr == DicomVR.ST)
                    items[0] = new DicomShortText(tag, characterSetCode, buffer);
                else if (vr == DicomVR.UC)
                    items[0] = new DicomUnlimitedCharacters(tag, characterSetCode, buffer);
                else if (vr == DicomVR.UR)
                    items[0] = new DicomUniversalResource(tag, characterSetCode, buffer);
                else if (vr == DicomVR.UT)
                    items[0] = new DicomUnlimitedText(tag, characterSetCode, buffer);
                else
                {
                    //Nothing 
                }

                dataset.AddOrUpdate(items);
                //byte[] byteValue = Encoding.Default.GetBytes(value);
                //dataset.AddOrUpdate<byte[]>(tag, byteValue);
            }

            //dobule, float
            if (vr == DicomVR.FD || vr == DicomVR.FL || vr == DicomVR.OF)
            {
                double dValue = Convert.ToDouble(value);
                dataset.AddOrUpdate<double>(tag, dValue);
            }

            //int
            if (vr == DicomVR.SL)
            {
                int iValue = Convert.ToInt32(value);
                dataset.AddOrUpdate<int>(tag, iValue);
            }

            //uint
            if (vr == DicomVR.UL)
            {
                uint uiValue = Convert.ToUInt32(value);
                dataset.AddOrUpdate<uint>(tag, uiValue);
            }

            //ushort
            if (vr == DicomVR.OW || vr == DicomVR.US)
            {
                ushort usValue = Convert.ToUInt16(value);
                dataset.AddOrUpdate<ushort>(tag, usValue);
            }

            //short
            if (vr == DicomVR.SS)
            {
                short sValue = Convert.ToInt16(value);
                dataset.AddOrUpdate<short>(tag, sValue);
            }
#pragma warning restore CS0618 // 類型或成員已經過時
        }

        #endregion

        #region RemoveItem

        /// <summary>
        /// 移除DicomItem
        /// </summary>
        /// <param name="dicomItems"></param>
        /// <param name="g"></param>
        /// <param name="e"></param>
        public void RemoveItem(DicomDataset dicomItems, ushort g, ushort e)
        {
            DicomTag dicomTag = new(g, e);
            dicomItems.Remove(dicomTag);
        }

        #endregion

        #region ConvertTagStringToUIntGE

        public void ConvertTagStringToUIntGE(string tag, out ushort group, out ushort elem)
        {
           
            group = 0;
            elem = 0;
            //判斷是否為Sequence的Tag
            int pos = tag.IndexOf(',');
            if (pos <= 0)
                return;
            //轉換成DICOM Tag
            string gggg = tag.Substring(0, pos);
            string eeee = tag.Substring(pos + 1, tag.Length - (pos + 1));
            group = Convert.ToUInt16(int.Parse(gggg, System.Globalization.NumberStyles.HexNumber));
            elem = Convert.ToUInt16(int.Parse(eeee, System.Globalization.NumberStyles.HexNumber));
        }

        #endregion

        #region ConfirmFileMetaInformation

        /// <summary>
        /// 確認FileMetaInformation資訊是否齊全
        /// </summary>
        /// <param name="metaInfo"></param>
        public void ConfirmFileMetaInformation(DicomFile dcmFile, DicomTag metaTag, DicomTag datasetTag)
        {
            DicomFileMetaInformation metaInfo = dcmFile.FileMetaInfo;
            DicomDataset dataset = dcmFile.Dataset;

#pragma warning disable CS0618 // 類型或成員已經過時
            //byte[] sopClassUID = Encoding.ASCII.GetBytes(dataset.Get<string>(tag: DicomTag.SOPClassUID));
            string tagValue = dataset.Get<string>(tag: datasetTag);
            if (metaInfo.Contains(metaTag) == false)
            {
                metaInfo.AddOrUpdate(metaTag, tagValue);
            }

            string value = metaInfo.Get<string>(metaTag, "");
            if (value.Trim() == "")
            {
                metaInfo.AddOrUpdate(metaTag, tagValue);
            }
#pragma warning restore CS0618 // 類型或成員已經過時
        }

        #endregion

        #region ConfirmFileMetaInformationWithValue

        /// <summary>
        /// 若MetaFileInformation沒有該Tag或有Tag但沒有值,直接填入資料
        /// </summary>
        /// <param name="dcmFile"></param>
        /// <param name="metaTag"></param>
        /// <param name="value"></param>
        public void ConfirmFileMetaInformationWithValue(DicomFile dcmFile, DicomTag metaTag, string tagValue)
        {
            DicomFileMetaInformation metaInfo = dcmFile.FileMetaInfo;
#pragma warning disable CS0618 // 類型或成員已經過時     
            if (metaInfo.Contains(metaTag) == false)
            {
                metaInfo.AddOrUpdate(metaTag, tagValue);
            }

            string value = metaInfo.Get<string>(metaTag, "");
            if (value.Trim() == "")
            {
                metaInfo.AddOrUpdate(metaTag, tagValue);
            }
#pragma warning restore CS0618 // 類型或成員已經過時
        }

        #endregion

        #region GetDicomValueToStringFromDicomItem

        /// <summary>
        /// 從DicomItem中取得資料並轉成字串
        /// </summary>
        /// <param name="dItem"></param>
        /// <returns></returns>
        public bool GetDicomValueToStringFromDicomItem(DicomItem dItem, DicomVR dVR, ref string value,
            bool isUtf8Encoding)
        {
            if (dItem == null)
                return false;

            try
            {
                if (dVR == DicomVR.AT)
                {
                    DicomAttributeTag dcmAttributeTag = dItem as DicomAttributeTag;
                    value = Convert.ToString(dcmAttributeTag.Buffer.Data);
                }
                else if (dVR == DicomVR.LT || dVR == DicomVR.ST || dVR == DicomVR.UR || dVR == DicomVR.UT ||
                         dVR == DicomVR.AS || dVR == DicomVR.AE ||
                         dVR == DicomVR.CS || dVR == DicomVR.UT || dVR == DicomVR.DA || dVR == DicomVR.DT ||
                         dVR == DicomVR.TM || dVR == DicomVR.DS ||
                         dVR == DicomVR.IS || dVR == DicomVR.LO || dVR == DicomVR.PN || dVR == DicomVR.SH ||
                         dVR == DicomVR.UI || dVR == DicomVR.UC)
                {
                    DicomStringElement dicomString = dItem as DicomStringElement;
                    if (isUtf8Encoding == true)
                        value = Encoding.UTF8.GetString(dicomString.Buffer.Data);
                    else
                    {
                        value = AnsiEncoding.GetString(dicomString.Buffer.Data);
                    }
                }
                else if (dVR == DicomVR.FD || dVR == DicomVR.FL || dVR == DicomVR.OF || dVR == DicomVR.OD ||
                         dVR == DicomVR.OL)
                {
                    DicomValueElement<double> valueElement = dItem as DicomValueElement<double>;
                    value = Convert.ToString(BitConverter.ToDouble(valueElement.Buffer.Data, 0));
                }
                else if (dVR == DicomVR.SL || dVR == DicomVR.SS)
                {
                    DicomSignedLong dicomSigned = dItem as DicomSignedLong;
                    value = Convert.ToString(BitConverter.ToInt32(dicomSigned.Buffer.Data, 0));
                }
                else if (dVR == DicomVR.US || dVR == DicomVR.UL)
                {
                    if (dItem is DicomUnsignedLong dicomULong)
                        value = Convert.ToString(BitConverter.ToUInt32(dicomULong.Buffer.Data, 0));
                }
                else
                {
                    value = "";
                }
            }
            catch (SystemException)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region DatasetToString

        public List<string> DatasetToString(DicomDataset dataset)
        {
            if (dataset == null)
                return null;
            bool isUtf8Encoding = false;
            //取得資料時,必需先確定要使用何種文字編碼                
            if (dataset.TryGetString(DicomTag.SpecificCharacterSet, out string elementVal) == true)
            {
                if (elementVal.Trim() == "ISO_IR 192" || elementVal.Trim() == "ISO_IR_192")
                    isUtf8Encoding = true;
            }

            List<string> results = new();
            //每一個Dataset之前先放一組間隔符號
            results.Add("");
            results.Add("--DICOM Dataset Start-----------------------------------------------------------------");

            string makeTagValueString(int idx, string tag, string vr, int len, string value)
            {
                int space0 = idx * 2;
                int space1 = 16 - space0;
                string strFormat = "{0," + Convert.ToString(space0 + 11) + "}{1," + Convert.ToString(space1) +
                                   "}{2,6}{3,40}";
                return (string.Format(strFormat, tag, vr, Convert.ToString(len), value));
            }

            //None Sequence Item
            void handleNonSequenceItem(DicomItem dcmItem, ref List<string> itemValues, int idx)
            {
                DicomTag dTag = dcmItem.Tag;
                DicomVR dVR = dcmItem.ValueRepresentation;
                DicomElement dElement = dcmItem as DicomElement;
                int length = (int)dElement.Length;
                string value = "";

                if (GetDicomValueToStringFromDicomItem(dcmItem, dVR, ref value, isUtf8Encoding) == true)
                {
                    itemValues.Add(makeTagValueString(idx, dTag.ToString("", null), dVR.ToString(), length, value));
                }
            }

            ;

            //Sequence Item
            void handleSequenceItem(DicomItem dcmItem, ref List<string> itemValues, int idx)
            {
                DicomSequence dSequence = dcmItem as DicomSequence;
                if (dSequence == null)
                    return;
                itemValues.Add(makeTagValueString(idx, dSequence.Tag.ToString("", null),
                    dSequence.ValueRepresentation.ToString(), 0, ""));

                ++idx;
                foreach (DicomDataset item in dSequence.Items)
                {
                    foreach (var subItem in item)
                    {
                        //區分巢狀和非巢狀
                        if (subItem.ValueRepresentation == DicomVR.SQ)
                            handleSequenceItem(subItem, ref itemValues, idx);
                        else
                            handleNonSequenceItem(subItem, ref itemValues, idx);
                    }
                }
            }

            foreach (var dcmItem in dataset)
            {
                //區分巢狀和非巢狀
                if (dcmItem.ValueRepresentation == DicomVR.SQ)
                    handleSequenceItem(dcmItem, ref results, 0);
                else
                    handleNonSequenceItem(dcmItem, ref results, 0);
            }

            results.Add("--DICOM Dataset End-------------------------------------------------------------------");
            return results;
        }

        #endregion

        public bool IsSequenceTag(DicomTag dicomTag)
        {
            var entry = DicomDictionary.Default[dicomTag];
            DicomVR vr = entry.ValueRepresentations.First();
            return vr == DicomVR.SQ;
        }

        public List<DataCorrection.V1.DcmTagData> ConvertDicomDatasetToDcmTagDataList(
            DicomDataset dataset,
            List<MappingTag> mappingTags,
            DicomOperatorHelper dcmHelper,
            bool isUtf8)
        {
            var dcmTagDataList = new List<DataCorrection.V1.DcmTagData>();

            foreach (var mappingTag in mappingTags)
            {
                // Mapping 記錄要轉換所有第一層Tag，如果有深層的Tag，就不處理
                if (mappingTag.ToTag.Contains("|")) continue;
                DicomTag dicomTag = DicomTag.Parse(mappingTag.ToTag);

                if (dataset.Contains(dicomTag) && dataset.GetDicomItem<DicomSequence>(dicomTag) != null)
                {
                    var sequence = dataset.GetDicomItem<DicomSequence>(dicomTag);
                    var seqDcmTagData = new DataCorrection.V1.DcmTagData
                    {
                        Keyword = sequence.Tag.DictionaryEntry.Keyword,
                        Name = sequence.Tag.ToString(),
                        Group = sequence.Tag.Group,
                        Elem = sequence.Tag.Element,
                        SeqDcmTagData = new List<DataCorrection.V1.DcmTagData>()
                    };

                    if (sequence.Items.Any())
                    {
                        foreach (var sequenceItem in sequence.Items)
                        {
                            ProcessSequenceItem(sequenceItem, seqDcmTagData, dcmHelper, isUtf8);
                        }
                        dcmTagDataList.Add(seqDcmTagData);
                    }
                }
                else
                {
                    var dcmTagData = new DataCorrection.V1.DcmTagData
                    {
                        Keyword = dicomTag.DictionaryEntry.Keyword,
                        Name = dicomTag.ToString(),
                        Group = dicomTag.Group,
                        Elem = dicomTag.Element,
                        SeqDcmTagData = new List<DataCorrection.V1.DcmTagData>(),
                        Value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, mappingTag.ToTag, isUtf8)
                    };

                    dcmTagDataList.Add(dcmTagData);
                }
            }

            return dcmTagDataList;
        }
        
        private void ProcessSequenceItem(DicomDataset sequenceItem, DataCorrection.V1.DcmTagData parentTagData, DicomOperatorHelper dcmHelper, bool isUtf8)
        {
            foreach (var dicomItem in sequenceItem)
            {
                var dicomTag = dicomItem.Tag;
                var seqDcmTagData = new DataCorrection.V1.DcmTagData
                {
                    Keyword = dicomTag.DictionaryEntry.Keyword,
                    Name = dicomTag.ToString(),
                    Group = dicomTag.Group,
                    Elem = dicomTag.Element,
                    SeqDcmTagData = new List<DataCorrection.V1.DcmTagData>()
                };

                if (dicomItem is DicomSequence subSequence)
                {
                    foreach (var subSequenceItem in subSequence.Items)
                    {
                        ProcessSequenceItem(subSequenceItem, seqDcmTagData, dcmHelper, isUtf8);
                    }
                }
                else
                {
                    var tagString = dicomTag.ToString().Replace("(","").Replace(")","");
                    seqDcmTagData.Value = dcmHelper.GetDicomValueToStringWithGroupAndElem(sequenceItem, tagString, isUtf8);
                }

                parentTagData.SeqDcmTagData.Add(seqDcmTagData);
            }
        }
    }
}