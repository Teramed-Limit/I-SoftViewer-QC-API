using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ISoftViewerLibrary.Models.DTOs
{
    public static class DataCorrection
    {
        public static class V1
        {
            #region CreateStudy<T>
            /// <summary>
            /// 檢查及影像歸檔資訊
            /// </summary>
            public class CreateAndModifyStudy<T>
                where T : ImageData, new()
            {
                /// <summary>
                /// 建構
                /// </summary>
                public CreateAndModifyStudy()
                {
                    PatientInfo = new PatientData();
                    StudyInfo = new List<StudyData>();
                    SeriesInfo = new List<SeriesData>();
                    ImageInfos = new List<T>();
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="patientInfo"></param>
                /// <param name="studyInfo"></param>
                public CreateAndModifyStudy(PatientData patientInfo, List<StudyData> studyInfo)
                {
                    PatientInfo = patientInfo;
                    StudyInfo = studyInfo;
                    SeriesInfo = new List<SeriesData>();
                    ImageInfos = new List<T>();
                }

                #region Fields
                /// <summary>
                /// 病患資訊
                /// </summary>
                [Required]
                public PatientData PatientInfo { get; set; }
                /// <summary>
                /// 檢查資訊
                /// </summary>
                [Required]
                public List<StudyData> StudyInfo { get; set; }
                /// <summary>
                /// 系列資訊
                /// </summary>
                [Required]
                public List<SeriesData> SeriesInfo { get; set; }
                /// <summary>
                /// 多組影像資訊
                /// </summary>
                [Required]
                public List<T> ImageInfos { get; set; }
                /// <summary>
                /// 多組影像資訊
                /// </summary>
                [Required]
                public bool SendOtherEnableNodes { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this);
                }
                #endregion
            }
            #endregion

            #region MergeStudyParameter
            /// <summary>
            /// 合併檢查校正參數
            /// </summary>
            public class MergeStudyParameter
            {
                /// <summary>
                /// 修改人員
                /// </summary>
                public string ModifyUser { get; set; } = "";
                /// <summary>
                /// 被執行QC的檢查
                /// </summary>
                public string FromStudyUID { get; set; } = "";
                /// <summary>
                /// 要對其它檢查QC的檢查
                /// </summary>
                public string ToStudyUID { get; set; } = "";
            }
            #endregion

            #region SplitStudyParameter
            /// <summary>
            /// 拆解檢查參數
            /// </summary>
            public class SplitStudyParameter
            {
                /// <summary>
                /// 修改人員
                /// </summary>
                public string ModifyUser { get; set; } = "";
                /// <summary>
                /// 被合併的檢查
                /// </summary>
                public string StudyInstanceUID { get; set; } = "";
                /// <summary>
                /// 是否在拆解檢查後刪除檔案
                /// </summary>
                public bool AfterSplitStudyToDeleteOldFiles { get; set; } = false;
            }
            #endregion

            #region StudyMappingParameter
            /// <summary>
            /// QC Mapping的參數
            /// </summary>
            public class StudyMappingParameter<T>
            {
                /// <summary>
                /// 條改使用者帳號
                /// </summary>
                public string ModifyUser { get; set; } = "";
                /// <summary>
                /// 前端的Dataset Tag列表
                /// </summary>
                public List<T> Dataset { get; set; } = new();
                /// <summary>
                /// 要Mapping的病歷號碼
                /// </summary>
                public string PatientId { get; set; }
                /// <summary>
                /// 要Mapping的檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; } = "";
                /// <summary>
                /// 目標的WorkList名稱，為了取得Mapping Rules
                /// </summary>
                public string TargetWorklistName { get; set; } = "";
            }
            #endregion
            

            #region StudyUnmappingParameter
            /// <summary>
            /// QC Unmapping參數
            /// </summary>
            public class StudyUnmappingParameter
            {
                /// <summary>
                /// 修改使用
                /// </summary>
                public string ModifyUser { get; set; } = "";
                /// <summary>
                /// 要Mapping的病歷號碼
                /// </summary>
                public string PatientId { get; set; }
                /// <summary>
                /// 要Mapping的檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; } = "";
            }
            #endregion

            #region DcmTagData
            /// <summary>
            /// 單筆DICOM Tag資料
            /// </summary>
            public class DcmTagData
            {
                /// <summary>
                /// 建構
                /// </summary>
                public DcmTagData()
                {
                    Group = 0;
                    Elem = 0;
                    Value = "";
                    Name = "";
                }

                #region Fields
                /// <summary>
                /// DICOM Tag Group
                /// </summary>
                public uint Group { get; set; }
                /// <summary>
                /// DICOM Tag Elem
                /// </summary>
                public uint Elem { get; set; }
                /// <summary>
                /// DICOM Tag 資料
                /// </summary>
                public string Value { get; set; }
                /// <summary>
                /// DICOM Tag Name
                /// </summary>
                public string Name { get; set; }
                /// <summary>
                /// DICOM Tag Keyword
                /// </summary>
                public string Keyword { get; set; }
                /// <summary>
                /// Rules
                /// </summary>
                public string MappingRules { get; set; }

                /// <summary>
                /// Sub Dcm TagData
                /// </summary>
                public List<DcmTagData> SeqDcmTagData { get; set; } = new ();

                #endregion
            }
            #endregion

            #region InformationEntity
            /// <summary>
            /// DICOM IE
            /// </summary>
            public class InformationEntity
            {
                /// <summary>
                /// 建構
                /// </summary>
                public InformationEntity()
                {
                    CustomizedFields = new List<DcmTagData>();
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="customizedFields"></param>
                public InformationEntity(List<DcmTagData> customizedFields)
                {
                    CustomizedFields = customizedFields;
                }
                #region Fields
                /// <summary>
                /// 客製化欄位
                /// </summary>
                public List<DcmTagData> CustomizedFields { get; set; }
                #endregion                
            }
            #endregion

            #region PatientIE
            /// <summary>
            /// Patient Information Entity
            /// </summary>
            public class PatientData : InformationEntity, ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public PatientData()
                    : base()
                {
                    PatientId = "";
                    PatientsName = "";
                    PatientsSex = "";
                    PatientsBirthDate = "";
                    PatientsBirthTime = "";
                    OtherPatientNames = "";
                    OtherPatientId = "";
                }
                public PatientData(string patientId, string patientsName)
                    : base()
                {
                    if (patientId == "")
                        throw new Exception("Patient ID cannot be empty");
                    PatientId = patientId;
                    PatientsName = patientsName;
                    PatientsSex = "";
                    PatientsBirthDate = "";
                    PatientsBirthTime = "";
                    OtherPatientNames = "";
                    OtherPatientId = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="patientId"></param>
                /// <param name="patientsName"></param>
                /// <param name="patientsSex"></param>
                /// <param name="patientsBirthDate"></param>
                /// <param name="patientsBirthTime"></param>
                /// <param name="otherPatientNames"></param>
                /// <param name="otherPatientId"></param>
                /// <param name="ethnicGroup"></param>
                /// <param name="patientComments"></param>
                protected PatientData(string patientId, string patientsName, string patientsSex, string patientsBirthDate,
                    string patientsBirthTime, string otherPatientNames, string otherPatientId, List<DcmTagData> fields)
                    : base(fields)
                {
                    if (patientId == "")
                        throw new Exception("Patient ID cannot be empty");

                    PatientId = patientId;
                    PatientsName = patientsName;
                    PatientsSex = patientsSex;
                    PatientsBirthDate = patientsBirthDate;
                    PatientsBirthTime = patientsBirthTime;
                    OtherPatientNames = otherPatientNames;
                    OtherPatientId = otherPatientId;
                }

                #region Fields
                /// <summary>
                /// 病歷號碼
                /// </summary>
                public string PatientId { get; set; }
                /// <summary>
                /// 病人姓名
                /// </summary>
                public string PatientsName { get; set; }
                /// <summary>
                /// 性別
                /// </summary>
                public string PatientsSex { get; set; }
                /// <summary>
                /// 出生日期
                /// </summary>
                public string PatientsBirthDate { get; set; }
                /// <summary>
                /// 出生時間
                /// </summary>
                public string PatientsBirthTime { get; set; }
                /// <summary>
                /// 其它病人姓名
                /// </summary>
                public string OtherPatientNames { get; set; }
                /// <summary>
                /// 其它病歷號碼
                /// </summary>
                public string OtherPatientId { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new PatientData(PatientId, PatientsName, PatientsSex, PatientsBirthDate, PatientsBirthTime, OtherPatientNames,
                                OtherPatientId, CustomizedFields);
                }
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this); ;
                }
                #endregion
            }
            #endregion

            #region StudyIE
            /// <summary>
            /// Study Information Entity
            /// </summary>
            public class StudyData : InformationEntity, ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public StudyData()
                    : base()
                {
                    StudyInstanceUID = "";
                    PatientId = "";
                    StudyDate = "";
                    StudyTime = "";
                    ReferringPhysiciansName = "";
                    StudyID = "";
                    AccessionNumber = "";
                    StudyDescription = "";
                    Modality = "";
                    PerformingPhysiciansName = "";
                    NameofPhysiciansReading = "";
                    ProcedureID = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="studyInstanceUID"></param>
                /// <param name="patientId"></param>
                public StudyData(string studyInstanceUID, string patientId)
                    : base()
                {
                    if (studyInstanceUID == "" || patientId == "")
                        throw new Exception("PatientID and StudyInstanceUID cannot be empty");

                    StudyInstanceUID = studyInstanceUID;
                    PatientId = patientId;
                    StudyDate = "";
                    StudyTime = "";
                    ReferringPhysiciansName = "";
                    StudyID = "";
                    AccessionNumber = "";
                    StudyDescription = "";
                    Modality = "OT";
                    PerformingPhysiciansName = "";
                    NameofPhysiciansReading = "";
                    ProcedureID = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="studyInstanceUID"></param>
                /// <param name="patientId"></param>
                /// <param name="studyDate"></param>
                /// <param name="studyTime"></param>
                /// <param name="referringPhysiciansName"></param>
                /// <param name="studyID"></param>
                /// <param name="accessionNumber"></param>
                /// <param name="studyDescription"></param>
                /// <param name="modality"></param>
                /// <param name="performingPhysiciansName"></param>
                /// <param name="nameofPhysiciansReading"></param>
                /// <param name="procedureID"></param>
                protected StudyData(string studyInstanceUID, string patientId, string studyDate, string studyTime, string referringPhysiciansName,
                    string studyID, string accessionNumber, string studyDescription, string modality, string performingPhysiciansName,
                    string nameofPhysiciansReading, string procedureID, List<DcmTagData> fields)
                    : base(fields)
                {
                    if (studyInstanceUID == "" || patientId == "")
                        throw new Exception("PatientID and StudyInstanceUID cannot be empty");

                    StudyInstanceUID = studyInstanceUID;
                    PatientId = patientId;
                    StudyDate = studyDate;
                    StudyTime = studyTime;
                    ReferringPhysiciansName = referringPhysiciansName;
                    StudyID = studyID;
                    AccessionNumber = accessionNumber;
                    StudyDescription = studyDescription;
                    Modality = modality;
                    PerformingPhysiciansName = performingPhysiciansName;
                    NameofPhysiciansReading = nameofPhysiciansReading;
                    ProcedureID = procedureID;
                }

                #region Fields
                /// <summary>
                /// 檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }
                /// <summary>
                /// 病歷姓名
                /// </summary>
                public string PatientId { get; set; }
                /// <summary>
                /// 檢查日期
                /// </summary>
                public string StudyDate { get; set; }
                /// <summary>
                /// 檢查時間
                /// </summary>
                public string StudyTime { get; set; }
                /// <summary>
                /// 主治醫師
                /// </summary>
                public string ReferringPhysiciansName { get; set; }
                /// <summary>
                /// 檢查編號
                /// </summary>
                public string StudyID { get; set; }
                /// <summary>
                /// 檢查單號
                /// </summary>
                public string AccessionNumber { get; set; }
                /// <summary>
                /// 檢查說明
                /// </summary>
                public string StudyDescription { get; set; }
                /// <summary>
                /// 儀器種類
                /// </summary>
                public string Modality { get; set; }
                /// <summary>
                /// 執行醫師姓名
                /// </summary>
                public string PerformingPhysiciansName { get; set; }
                /// <summary>
                /// 報告醫師姓名
                /// </summary>
                public string NameofPhysiciansReading { get; set; }
                /// <summary>
                /// 檢查程序代碼
                /// </summary>
                public string ProcedureID { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new StudyData(StudyInstanceUID, PatientId, StudyDate, StudyTime, ReferringPhysiciansName, StudyID, AccessionNumber,
                        StudyDescription, Modality, PerformingPhysiciansName, NameofPhysiciansReading, ProcedureID, CustomizedFields);
                }
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this); ;
                }
                #endregion
            }
            #endregion

            #region SeriesIE
            /// <summary>
            /// Series Information Entity
            /// </summary>
            public class SeriesData : InformationEntity, ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public SeriesData()
                    : base()
                {
                    SeriesInstanceUID = "";
                    StudyInstanceUID = "";
                    SeriesModality = "";
                    SeriesDate = "";
                    SeriesTime = "";
                    SeriesNumber = "";
                    SeriesDescription = "";
                    PatientPosition = "";
                    BodyPartExamined = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="seriesInstanceUID"></param>
                /// <param name="studyInstanceUID"></param>
                public SeriesData(string seriesInstanceUID, string studyInstanceUID)
                    : base()
                {
                    if (SeriesInstanceUID == "" || StudyInstanceUID == "")
                        throw new Exception("SeriesInstanceUID and StudyInstanceUID cannot be empty");
                    SeriesInstanceUID = seriesInstanceUID;
                    StudyInstanceUID = studyInstanceUID;
                    SeriesModality = "";
                    SeriesDate = "";
                    SeriesTime = "";
                    SeriesNumber = "";
                    SeriesDescription = "";
                    PatientPosition = "";
                    BodyPartExamined = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="seriesInstanceUID"></param>
                /// <param name="studyInstanceUID"></param>
                /// <param name="seriesModality"></param>
                /// <param name="seriesDate"></param>
                /// <param name="seriesTime"></param>
                /// <param name="seriesNumber"></param>
                /// <param name="seriesDescription"></param>
                /// <param name="patientPosition"></param>
                /// <param name="bodyPartExamined"></param>
                public SeriesData(string seriesInstanceUID, string studyInstanceUID, string seriesModality, string seriesDate,
                    string seriesTime, string seriesNumber, string seriesDescription, string patientPosition, string bodyPartExamined,
                    List<DcmTagData> fields)
                    : base(fields)
                {
                    if (SeriesInstanceUID == "" || StudyInstanceUID == "")
                        throw new Exception("SeriesInstanceUID and StudyInstanceUID cannot be empty");
                    SeriesInstanceUID = seriesInstanceUID;
                    StudyInstanceUID = studyInstanceUID;
                    SeriesModality = seriesModality;
                    SeriesDate = seriesDate;
                    SeriesTime = seriesTime;
                    SeriesNumber = seriesNumber;
                    SeriesDescription = seriesDescription;
                    PatientPosition = patientPosition;
                    BodyPartExamined = bodyPartExamined;
                }

                #region Fields
                /// <summary>
                /// 系列唯一碼
                /// </summary>
                public string SeriesInstanceUID { get; set; }
                /// <summary>
                /// 檢查唯一碼
                /// </summary>
                public string StudyInstanceUID { get; set; }
                /// <summary>
                /// 儀器種類
                /// </summary>
                public string SeriesModality { get; set; }
                /// <summary>
                /// 系列日期
                /// </summary>
                public string SeriesDate { get; set; }
                /// <summary>
                /// 系列時間
                /// </summary>
                public string SeriesTime { get; set; }
                /// <summary>
                /// 系列編號
                /// </summary>
                public string SeriesNumber { get; set; }
                /// <summary>
                /// 系列說明
                /// </summary>
                public string SeriesDescription { get; set; }
                /// <summary>
                /// 病人拍攝位置
                /// </summary>
                public string PatientPosition { get; set; }
                /// <summary>
                /// 拍攝部位
                /// </summary>
                public string BodyPartExamined { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public object Clone()
                {
                    return new SeriesData(SeriesInstanceUID, StudyInstanceUID, SeriesModality, SeriesDate, SeriesTime, SeriesNumber,
                        SeriesDescription, PatientPosition, BodyPartExamined, CustomizedFields);
                }
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this); ;
                }
                #endregion
            }
            #endregion

            #region ImageIE
            /// <summary>
            /// Image Information Entity
            /// </summary>
            public class ImageData : ICloneable
            {
                /// <summary>
                /// 建構
                /// </summary>
                public ImageData()
                {
                    SOPInstanceUID = "";
                    SeriesInstanceUID = "";
                    SOPClassUID = "";
                    ImageNumber = "";
                    ImageDate = "";
                    ImageTime = "";
                    WindowWidth = "";
                    WindowCenter = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="sopInstanceUID"></param>
                /// <param name="seriesInstanceUID"></param>
                /// <param name="sopClassUID"></param>
                public ImageData(string sopInstanceUID, string seriesInstanceUID, string sopClassUID)
                    : base()
                {
                    if (sopInstanceUID == "" || seriesInstanceUID == "" || sopClassUID == "")
                        throw new Exception("SOPInstanceUID and SeriesInstanceUID and SOPClassUID cannot be empty");

                    SOPInstanceUID = sopInstanceUID;
                    SeriesInstanceUID = seriesInstanceUID;
                    SOPClassUID = sopClassUID;
                    ImageNumber = "";
                    ImageDate = "";
                    ImageTime = "";
                    WindowWidth = "";
                    WindowCenter = "";
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="sopInstanceUID"></param>
                /// <param name="seriesInstanceUID"></param>
                /// <param name="sopClassUID"></param>
                /// <param name="imageNumber"></param>
                /// <param name="imageDate"></param>
                /// <param name="imageTime"></param>
                /// <param name="windowWidth"></param>
                /// <param name="windowCenter"></param>
                protected ImageData(string sopInstanceUID, string seriesInstanceUID, string sopClassUID, string imageNumber, string imageDate,
                    string imageTime, string windowWidth, string windowCenter)
                {
                    if (sopInstanceUID == "" || seriesInstanceUID == "" || sopClassUID == "")
                        throw new Exception("SOPInstanceUID and SeriesInstanceUID and SOPClassUID cannot be empty");

                    SOPInstanceUID = sopInstanceUID;
                    SeriesInstanceUID = seriesInstanceUID;
                    SOPClassUID = sopClassUID;
                    ImageNumber = imageNumber;
                    ImageDate = imageDate;
                    ImageTime = imageTime;
                    WindowWidth = windowWidth;
                    WindowCenter = windowCenter;
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="image"></param>
                protected ImageData(ImageData image)
                {
                    SOPInstanceUID = image.SOPInstanceUID;
                    SeriesInstanceUID = image.SeriesInstanceUID;
                    SOPClassUID = image.SOPClassUID;
                    ImageNumber = image.ImageNumber;
                    ImageDate = image.ImageDate;
                    ImageTime = image.ImageTime;
                    WindowWidth = image.WindowWidth;
                    WindowCenter = image.WindowCenter;
                }

                #region Fields
                /// <summary>
                /// 影像唯一碼
                /// </summary>
                public string SOPInstanceUID { get; set; }
                /// <summary>
                /// 系列唯一碼
                /// </summary>
                public string SeriesInstanceUID { get; set; }
                /// <summary>
                /// 影像種類碼
                /// </summary>
                public string SOPClassUID { get; set; }
                /// <summary>
                /// 影像編號
                /// </summary>
                public string ImageNumber { get; set; }
                /// <summary>
                /// 影像日期
                /// </summary>
                public string ImageDate { get; set; }
                /// <summary>
                /// 影像時間
                /// </summary>
                public string ImageTime { get; set; }
                /// <summary>
                /// 影像灰階範圍
                /// </summary>
                public string WindowWidth { get; set; }
                /// <summary>
                /// 影像灰階中心值
                /// </summary>
                public string WindowCenter { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public virtual object Clone()
                {
                    return new ImageData(SOPInstanceUID, SeriesInstanceUID, SOPClassUID, ImageNumber, ImageDate, ImageTime,
                        WindowWidth, WindowCenter);
                }
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this); ;
                }
                #endregion
            }
            #endregion

            #region ImageBufferAndData
            /// <summary>
            /// 影像記憶體類型
            /// </summary>
            public enum BufferType { btDcm = 0, btBmp = 1, btJpg = 2, btPng = 3 };
            /// <summary>
            /// 包含影像記憶體的影像實體
            /// </summary>
            public class ImageBufferAndData : ImageData
            {
                /// <summary>
                /// 建構
                /// </summary>
                public ImageBufferAndData()
                    : base()
                {
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="sopInstanceUID"></param>
                /// <param name="seriesInstanceUID"></param>
                /// <param name="sopClassUID"></param>
                public ImageBufferAndData(string sopInstanceUID, string seriesInstanceUID, string sopClassUID)
                    : base(sopInstanceUID, seriesInstanceUID, sopClassUID)
                {
                }
                /// <summary>
                /// 建構
                /// </summary>
                /// <param name="data"></param>
                /// <param name="buffer"></param>
                protected ImageBufferAndData(ImageData data, byte[] buffer)
                    : base(data)
                {
                    Buffer = buffer.ToArray();
                }

                #region Fields
                /// <summary>
                /// 影像記憶體
                /// </summary>
                public byte[] Buffer { get; set; }
                /// <summary>
                /// 影像記憶體類型
                /// </summary>
                public BufferType Type { get; set; }
                #endregion

                #region Methods
                /// <summary>
                /// 副本
                /// </summary>
                /// <returns></returns>
                public override object Clone()
                {
                    return new ImageBufferAndData(base.Clone() as ImageData, Buffer);
                }
                /// <summary>
                /// 轉成字串
                /// </summary>
                /// <returns></returns>
                public override string ToString()
                {
                    return JsonSerializer.Serialize(this); ;
                }
                #endregion
            }
            #endregion
        }
    }
}
