using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Models.Events;
using System.Collections;
using ISoftViewerLibrary.Models.Interfaces;
using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;

namespace ISoftViewerLibrary.Models.Entity
{
    public static class DicomEntities
    {
        #region PatientEntity
        /// <summary>
        /// Patient Information Object Entity Definition
        /// </summary>
        public class PatientEntity : Entity<DicomSourceReference, DcmString>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="applier"></param>
            public PatientEntity(Action<object> applier) : base(applier)
            {
            }

            #region Fields
            /// <summary>
            /// 病歷號碼
            /// </summary>
            public DcmString PatientId { get; private set; }
            /// <summary>
            /// 病人姓名
            /// </summary>
            public DcmString PatientsName { get; private set; }
            /// <summary>
            /// 性別
            /// </summary>
            public DcmString PatientsSex { get; private set; }
            /// <summary>
            /// 出生日期
            /// </summary>
            public DcmString PatientsBirthDate { get; private set; }
            /// <summary>
            /// 出生時間
            /// </summary>
            public DcmString PatientsBirthTime { get; private set; }
            /// <summary>
            /// 其它病人姓名
            /// </summary>
            public DcmString OtherPatientNames { get; private set; }
            /// <summary>
            /// 其它病歷號碼
            /// </summary>
            public DcmString OtherPatientId { get; private set; }
            /// <summary>
            /// 其它Tag資訊
            /// </summary>
            public List<DcmString> OtherTags { get; private set; }
            #endregion

            #region Methods  
            /// <summary>
            /// 取得Patient參數
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<DcmString> GetEnumerator()
            {
                yield return PatientId;
                yield return PatientsName;

                if (PatientsSex != null) yield return PatientsSex;
                if (PatientsSex != null) yield return PatientsBirthDate;
                if (PatientsSex != null) yield return PatientsBirthTime;
                if (PatientsSex != null) yield return OtherPatientNames;
                if (PatientsSex != null) yield return OtherPatientId;

                if (OtherTags == null)
                    yield break;

                foreach (var item in OtherTags)
                    yield return item;
            }
            /// <summary>
            /// Entity事件觸發
            /// </summary>
            /// <param name="event"></param>
            protected override void When(object @event)
            {
                switch (@event)
                {
                    case DcmEvents.OnPatientCreated e:
                        Id = new DicomSourceReference(e.Id);
                        PatientId = new DcmString("0010,0020", e.PatientId, "PatientId");
                        PatientsName = new DcmString("0010,0010", e.NormalKeys.PatientsName, "PatientsName");
                        PatientsSex = new DcmString("0010,0040", e.NormalKeys.PatientsSex, "PatientsSex");
                        PatientsBirthDate = new DcmString("0010,0030", e.NormalKeys.PatientsBirthDate, "PatientsBirthDate");
                        PatientsBirthTime = new DcmString("0010,0032", e.NormalKeys.PatientsBirthTime, "PatientsBirthTime");
                        OtherPatientNames = new DcmString("0010,1001", e.NormalKeys.OtherPatientNames, "OtherPatientNames");
                        OtherPatientId = new DcmString("0010,1000", e.NormalKeys.OtherPatientId, "OtherPatientId");
                        OtherTags = new List<DcmString>();
                        e.NormalKeys.DcmOtherTags.ForEach(x => OtherTags.Add(new DcmString((ushort)x.Group, (ushort)x.Elem, x.Value, "")));
                        break;                    
                    case DcmEvents.OnPatientUpdated e:
                        PatientsName = new DcmString("0010,0010", e.PatientsName, "PatientsName");
                        PatientsSex = new DcmString("0010,0040", e.PatientsSex, "PatientsSex");
                        PatientsBirthDate = new DcmString("0010,0030", e.PatientsBirthDate, "PatientsBirthDate");
                        PatientsBirthTime = new DcmString("0010,0032", e.PatientsBirthTime, "PatientsBirthTime");
                        OtherPatientNames = new DcmString("0010,1001", e.OtherPatientNames, "OtherPatientNames");
                        OtherPatientId = new DcmString("0010,1000", e.OtherPatientId, "OtherPatientId");
                        break;
                    case DcmEvents.OnScheduledProcedureStepCreated e:
                        Id = new DicomSourceReference(e.Id);
                        PatientId = new DcmString("0010,0020", e.PatientId, "PatientId");
                        PatientsName = new DcmString("0010,0010", e.PatientName, "PatientsName");
                        break;
                }
            }            
            #endregion
        }
        #endregion

        #region StudyEntity
        /// <summary>
        /// Study Information Object Entity Definition
        /// </summary>
        public class StudyEntity : Entity<DicomSourceReference, DcmString>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="applier"></param>
            public StudyEntity(Action<object> applier) : base(applier)
            {
            }

            #region Fields
            /// <summary>
            /// 檢查唯一碼
            /// </summary>
            public DcmString StudyInstanceUID { get; private set; }
            /// <summary>
            /// 病歷號碼
            /// </summary>
            public DcmString PatientId { get; private set; }
            /// <summary>
            /// 檢查日期
            /// </summary>
            public DcmString StudyDate { get; private set; }
            /// <summary>
            /// 檢查時間
            /// </summary>
            public DcmString StudyTime { get; private set; }
            /// <summary>
            /// 主治醫師
            /// </summary>
            public DcmString ReferringPhysiciansName { get; private set; }
            /// <summary>
            /// 檢查編號
            /// </summary>
            public DcmString StudyID { get; private set; }
            /// <summary>
            /// 檢查單號
            /// </summary>
            public DcmString AccessionNumber { get; private set; }
            /// <summary>
            /// 檢查說明
            /// </summary>
            public DcmString StudyDescription { get; private set; }
            /// <summary>
            /// 儀器
            /// </summary>
            public DcmString Modality { get; private set; }
            /// <summary>
            /// 執行醫師
            /// </summary>
            public DcmString PerformingPhysiciansName { get; private set; }
            /// <summary>
            /// 報告醫師
            /// </summary>
            public DcmString NameofPhysiciansReading { get; private set; }
            /// <summary>
            /// 程序編號
            /// </summary>
            public DcmString ProcedureID { get; private set; }
            /// <summary>
            /// 其它Tag資訊
            /// </summary>
            public List<DcmString> OtherTags { get; private set; }
            #endregion

            #region Methods
            /// <summary>
            /// 取得Study參數
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<DcmString> GetEnumerator()
            {
                yield return StudyInstanceUID;
                yield return Modality;

                yield return AccessionNumber;
                yield return StudyDate;
                
                yield return StudyDescription;
                yield return PerformingPhysiciansName;                
                yield return ProcedureID;

                if (StudyTime != null) yield return StudyTime;
                if (ReferringPhysiciansName != null) yield return ReferringPhysiciansName;
                if (StudyID != null) yield return StudyID;
                if (NameofPhysiciansReading != null) yield return NameofPhysiciansReading;

                if (OtherTags == null) yield break;

                foreach (var item in OtherTags)
                    yield return item;
            }
            /// <summary>
            /// 事件觸發
            /// </summary>
            /// <param name="event"></param>
            protected override void When(object @event)
            {
                switch (@event)
                {
                    case DcmEvents.OnStudyCreated e:
                        Id = new DicomSourceReference(e.Id);
                        StudyInstanceUID = new DcmString("0020,000D", e.StudyInstanceUID, "StudyInstanceUID");
                        PatientId = new DcmString("0010,0020", e.PatientId, "PatientId");
                        StudyDate = new DcmString("0008,0020", e.NormalKeys.StudyDate, "StudyDate");
                        StudyTime = new DcmString("0008,0030", e.NormalKeys.StudyTime, "StudStudyTimeyDate");
                        ReferringPhysiciansName = new DcmString("0008,0090", e.NormalKeys.ReferringPhysiciansName, "ReferringPhysiciansName");
                        StudyID = new DcmString("0020,0010", e.NormalKeys.StudyID, "StudyID");
                        AccessionNumber = new DcmString("0008,0050", e.NormalKeys.AccessionNumber, "AccessionNumber");
                        StudyDescription = new DcmString("0008,1030", e.NormalKeys.StudyDescription, "StudyDescription");
                        Modality = new DcmString("0008,0060", e.NormalKeys.Modality, "Modality");
                        PerformingPhysiciansName = new DcmString("0008,1050", e.NormalKeys.PerformingPhysiciansName, "PerformingPhysiciansName");
                        NameofPhysiciansReading = new DcmString("0008,1060", e.NormalKeys.NameofPhysiciansReading, "NameofPhysiciansReading");
                        ProcedureID = new DcmString("0040,1001", e.NormalKeys.ProcedureID, "ProcedureID");
                        OtherTags = new List<DcmString>();
                        e.NormalKeys.DcmOtherTags.ForEach(x => OtherTags.Add(new DcmString((ushort)x.Group, (ushort)x.Elem, x.Value, "")));
                        break;
                    case DcmEvents.OnStudyUpdated e:
                        StudyDate = new DcmString("0008,0020", e.StudyDate, "StudyDate");
                        StudyTime = new DcmString("0008,0030", e.StudyTime, "StudStudyTimeyDate");
                        ReferringPhysiciansName = new DcmString("0008,0090", e.ReferringPhysiciansName, "ReferringPhysiciansName");
                        StudyID = new DcmString("0020,0010", e.StudyID, "StudyID");
                        AccessionNumber = new DcmString("0008,0050", e.AccessionNumber, "AccessionNumber");
                        StudyDescription = new DcmString("0008,1030", e.StudyDescription, "StudyDescription");
                        Modality = new DcmString("0008,0060", e.Modality, "Modality");
                        PerformingPhysiciansName = new DcmString("0008,1050", e.PerformingPhysiciansName, "PerformingPhysiciansName");
                        NameofPhysiciansReading = new DcmString("0008,1060", e.NameofPhysiciansReading, "NameofPhysiciansReading");
                        ProcedureID = new DcmString("0040,1001", e.ProcedureID, "ProcedureID");
                        break;
                    case DcmEvents.OnScheduledProcedureStepCreated e:
                        Id = new DicomSourceReference(e.Id);
                        StudyInstanceUID = new DcmString("0020,000D", e.StudyInstanceUID, "StudyInstanceUID");
                        PatientId = new DcmString("0010,0020", e.PatientId, "PatientId");
                        StudyDate = new DcmString("0040,0002", e.StudyDate, "StudyDate");                        
                        AccessionNumber = new DcmString("0008,0050", e.AccessionNumber, "AccessionNumber");
                        StudyDescription = new DcmString("0040,0007", e.StudyDescription, "StudyDescription");
                        Modality = new DcmString("0008,0060", e.Modality, "Modality");
                        PerformingPhysiciansName = new DcmString("0040,0006", e.PerformingPhysiciansName, "PerformingPhysiciansName");                        
                        ProcedureID = new DcmString("0040,0009", e.ProcedureID, "ProcedureID");
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region SeriesEntity
        /// <summary>
        /// Seires Information Object Entity Definition
        /// </summary>
        public class SeriesEntity : Entity<DicomSourceReference, DcmString>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="applier"></param>
            public SeriesEntity(Action<object> applier) : base(applier)
            {
            }

            #region Fields
            /// <summary>
            /// 檢查唯一碼
            /// </summary>
            public DcmString StudyInstanceUID { get; private set; }
            /// <summary>
            /// 系列唯一碼
            /// </summary>
            public DcmString SeriesInstanceUID { get; private set; }
            /// <summary>
            /// 儀器
            /// </summary>
            public DcmString SeriesModality { get; private set; }
            /// <summary>
            /// 系列日期
            /// </summary>
            public DcmString SeriesDate { get; private set; }
            /// <summary>
            /// 系列時間
            /// </summary>
            public DcmString SeriesTime { get; private set; }
            /// <summary>
            /// 系列編號
            /// </summary>
            public DcmString SeriesNumber { get; private set; }
            /// <summary>
            /// 系列說明
            /// </summary>
            public DcmString SeriesDescription { get; private set; }
            /// <summary>
            /// 病人拍攝位置
            /// </summary>
            public DcmString PatientPosition { get; private set; }
            /// <summary>
            /// 拍攝部份
            /// </summary>
            public DcmString BodyPartExamined { get; private set; }
            /// <summary>
            /// 其它Tag資訊
            /// </summary>
            public List<DcmString> OtherTags { get; private set; }
            #endregion

            #region Methods
            /// <summary>
            /// 取得Series參數
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<DcmString> GetEnumerator()
            {                
                yield return SeriesInstanceUID;
                yield return SeriesModality;

                yield return SeriesDate;
                yield return SeriesTime;
                yield return SeriesNumber;
                yield return SeriesDescription;
                yield return PatientPosition;
                yield return BodyPartExamined;

                foreach (var item in OtherTags)
                    yield return item;                
            }
            /// <summary>
            /// 事件觸發
            /// </summary>
            /// <param name="event"></param>
            protected override void When(object @event)
            {
                switch (@event)
                {
                    case DcmEvents.OnSeriesCreated e:
                        Id = new DicomSourceReference(e.Id);
                        StudyInstanceUID = new DcmString("0020,000D", e.StudyInstanceUID, "StudyInstanceUID");
                        SeriesInstanceUID = new DcmString("0020,000E", e.SeriesInstanceUID, "SeriesInstanceUID");
                        SeriesModality = new DcmString("0008,0060", e.NormalKeys.SeriesModality, "SeriesModality");
                        SeriesDate = new DcmString("0008,0021", e.NormalKeys.SeriesDate, "SeriesDate");
                        SeriesTime = new DcmString("0008,0031", e.NormalKeys.SeriesTime, "SeriesTime");
                        SeriesNumber = new DcmString("0020,0011", e.NormalKeys.SeriesNumber, "SeriesNumber");
                        SeriesDescription = new DcmString("0008,103E", e.NormalKeys.SeriesDescription, "SeriesDescription");
                        PatientPosition = new DcmString("0018,5100", e.NormalKeys.PatientPosition, "PatientPosition");
                        BodyPartExamined = new DcmString("0018,0015", e.NormalKeys.BodyPartExamined, "BodyPartExamined");
                        OtherTags = new List<DcmString>();
                        e.NormalKeys.DcmOtherTags.ForEach(x => OtherTags.Add(new DcmString((ushort)x.Group, (ushort)x.Elem, x.Value, "")));
                        break;
                    case DcmEvents.OnSeriesUpdated e:
                        SeriesModality = new DcmString("0008,0060", e.SeriesModality, "SeriesModality");
                        SeriesDate = new DcmString("0008,0021", e.SeriesDate, "SeriesDate");
                        SeriesTime = new DcmString("0008,0031", e.SeriesTime, "SeriesTime");
                        SeriesNumber = new DcmString("0020,0011", e.SeriesNumber, "SeriesNumber");
                        SeriesDescription = new DcmString("0008,103E", e.SeriesDescription, "SeriesDescription");
                        PatientPosition = new DcmString("0018,5100", e.PatientPosition, "PatientPosition");
                        BodyPartExamined = new DcmString("0018,0015", e.BodyPartExamined, "BodyPartExamined");
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region ImageEntity
        /// <summary>
        /// Image Information Object Entity Definition
        /// </summary>
        public class ImageEntity : Entity<DicomSourceReference, DcmString>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="applier"></param>
            public ImageEntity(Action<object> applier) : base(applier)
            {
            }

            #region Fields
            /// <summary>
            /// 影像唯一碼
            /// </summary>
            public DcmString SOPInstanceUID { get; private set; }
            /// <summary>
            /// 系列唯一碼
            /// </summary>
            public DcmString SeriesInstanceUID { get; private set; }
            /// <summary>
            /// 影像種類碼
            /// </summary>
            public DcmString SOPClassUID { get; private set; }
            /// <summary>
            /// 影像編號
            /// </summary>
            public DcmString ImageNumber { get; private set; }
            /// <summary>
            /// 影像日期
            /// </summary>
            public DcmString ImageDate { get; private set; }
            /// <summary>
            /// 影像時間
            /// </summary>
            public DcmString ImageTime { get; private set; }
            /// <summary>
            /// 灰階影像範圍
            /// </summary>
            public DcmString WindowWidth { get; private set; }
            /// <summary>
            /// 灰階影像中間值
            /// </summary>
            public DcmString WindowCenter { get; private set; }
            /// <summary>
            /// 影像記憶體
            /// </summary>
            public byte[] ImageBuffer { get; private set; }
            /// <summary>
            /// 是否為DICOM格式
            /// </summary>
            public bool IsDcmImageBuffer { get; private set; }
            #endregion

            #region Methods
            /// <summary>
            /// 取得Image參數
            /// </summary>
            /// <returns></returns>
            public override IEnumerable<DcmString> GetEnumerator()
            {                
                yield return SOPInstanceUID;
                yield return SOPClassUID;
                yield return ImageNumber;
                yield return ImageDate;
                yield return ImageTime;

                yield return WindowWidth;
                yield return WindowCenter;
                yield break;
            }
            /// <summary>
            /// 事件觸發
            /// </summary>
            /// <param name="event"></param>
            protected override void When(object @event)
            {
                switch (@event)
                {
                    case DcmEvents.OnImageCreated e:
                        Id = new DicomSourceReference(e.Id);
                        SOPInstanceUID = new DcmString("0008,0018", e.SOPInstanceUID, "SOPInstanceUID");
                        SeriesInstanceUID = new DcmString("0020,000E", e.SeriesInstanceUID, "SeriesInstanceUID");
                        SOPClassUID = new DcmString("0008,0016", e.SOPClassUID, "SOPClassUID");
                        ImageNumber = new DcmString("0020,0013", e.NormalKeys.ImageNumber, "ImageNumber");
                        ImageDate = new DcmString("0008,0023", e.NormalKeys.ImageDate, "ImageDate");
                        ImageTime = new DcmString("0008,0033", e.NormalKeys.ImageTime, "ImageTime");
                        WindowWidth = new DcmString("0028,1051", e.NormalKeys.WindowWidth, "WindowWidth");
                        WindowCenter = new DcmString("0028,1050", e.NormalKeys.WindowCenter, "WindowCenter");
                        IsDcmImageBuffer = e.IsDcmBuffer;
                        break;
                    case DcmEvents.OnImageUpdated e:
                        ImageNumber = new DcmString("0020,0013", e.ImageNumber, "ImageNumber");
                        ImageDate = new DcmString("0008,0023", e.ImageDate, "ImageDate");
                        ImageTime = new DcmString("0008,0033", e.ImageTime, "ImageTime");
                        WindowWidth = new DcmString("0028,1051", e.WindowWidth, "WindowWidth");
                        WindowCenter = new DcmString("0028,1050", e.WindowCenter, "WindowCenter");
                        break;
                    case DcmEvents.OnImageBufferUpdated e:
                        ImageBuffer = e.ImageBuffer;                        
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region DicomSource
        /// <summary>
        /// DICOM源頭
        /// </summary>
        public class DicomSourceReference : Value<DicomSourceReference>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="value"></param>
            public DicomSourceReference(Guid id)
            {                
                Id = id;
            }

            #region Fields            
            /// <summary>
            /// 程式唯一碼
            /// </summary>
            public Guid Id { get; }
            #endregion            
        }
        #endregion

        #region DcmDataWrapper<T1>
        /// <summary>
        /// DICOM檔案封裝
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        public class DcmDataWrapper<T1> : IDcmDataWrapper<T1>, IOpMessage, IDisposable
            where T1 : Entity<DicomSourceReference, DcmString>
        {
            /// <summary>
            /// 建構
            /// </summary>
            public DcmDataWrapper(bool byPassEmptyValue = true, bool removeTagWhenByPassEmptyValue = true)
            {
                ByPassEmptyValue = byPassEmptyValue;
                RemoveTagWhenByPassEmptyValue = removeTagWhenByPassEmptyValue;
            }

            #region Fields
            /// <summary>
            /// 是否要略過空值
            /// </summary>
            private readonly bool ByPassEmptyValue;
            /// <summary>
            /// 略過空值時，是否刪除Tag
            /// </summary>
            private readonly bool RemoveTagWhenByPassEmptyValue;
            /// <summary>
            /// 訊息
            /// </summary>
            public string Message { get; private set; }
            /// <summary>
            /// 處理結果
            /// </summary>
            public OpResult Result { get; private set; }
            #endregion

            #region Methods
            /// <summary>
            /// 資料封裝
            /// </summary>
            /// <param name="dicomFile"></param>
            /// <param name="entity"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool DataWrapper(DicomDataset dcmDataset, T1 entity, DcmString key)
            {
                try 
                {
                    DicomOperatorHelper dcmHelper = new();
                    DicomDataset dataset = dcmDataset;
                    foreach (DcmString item in entity.GetEnumerator())
                    {
                        //有資料就寫入,沒有資料就移除
                        if (!string.IsNullOrEmpty(item.Value))
                        {
                            dcmHelper.WriteDicomValueInDataset(dataset, new DicomTag(item.TagGroup, item.TagElem), item.Value, true);
                        }                            
                        else
                        {
                            //要在確認是否要封裝空值到指定的Tag裡,或是要移除Tag
                            if (ByPassEmptyValue == false)
                                dcmHelper.WriteDicomValueInDataset(dataset, new DicomTag(item.TagGroup, item.TagElem), "", true);
                            else
                            {
                                if(RemoveTagWhenByPassEmptyValue)
                                    dcmHelper.RemoveItem(dataset, item.TagGroup, item.TagElem);
                            }
                        }                        
                    }
                    if (key != null)
                    {
                        //連外鍵Tag一定會有值,因為前段程式已有驗證
                        dcmHelper.WriteDicomValueInDataset(dataset, new DicomTag(key.TagGroup, key.TagElem), key.Value, true);
                    }                    
                }
                catch (Exception ex)
                {
                    Message = ex.Message;
                    Result = OpResult.OpFailure;
                }
                return true;
            }            
            /// <summary>
            /// 資料封裝
            /// </summary>
            /// <param name="dicomFile"></param>
            /// <param name="entities"></param>
            /// <param name="key"></param>
            /// <returns></returns>
            public bool DataWrapper(DicomDataset dcmDataset, Func<DcmString, T1> getEntityFunc, Func<T1, DcmString> GetUiqueKeyFunc, 
                DcmString selfKey)
            {         
                //取得目前的DicomEntity
                T1 entity = getEntityFunc(selfKey);
                //取得上層UID
                DcmString key = GetUiqueKeyFunc(entity);

                using DcmDataWrapper<T1> dcmDataWrapper = new(ByPassEmptyValue, RemoveTagWhenByPassEmptyValue);
                if (dcmDataWrapper.DataWrapper(dcmDataset, entity, key) == false)
                    return false;

                return true;
            }                        
            /// <summary>
            /// 垃圾回收
            /// </summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }
            #endregion
        }
        #endregion
    }
}
