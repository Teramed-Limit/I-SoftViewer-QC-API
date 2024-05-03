using Dicom;
using Dicom.IO;
using Dicom.IO.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Model.DicomOperator
{
    public class DicomOperatorHelper
    {
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
                        value = Encoding.UTF8.GetString(element.Buffer.Data);
                    else
                    {
                        var big5Encoding = Encoding.GetEncoding(950);
                        value = big5Encoding.GetString(element.Buffer.Data);
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
                    characterSetCode = Encoding.GetEncoding("big5");
                    buffer = ByteConverter.ToByteBuffer(value ?? String.Empty, Encoding.GetEncoding("big5"), 32);
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
                        var big5Encoding = Encoding.GetEncoding(950);
                        value = big5Encoding.GetString(dicomString.Buffer.Data);
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
    }
}