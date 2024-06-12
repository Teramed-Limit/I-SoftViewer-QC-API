using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.Exceptions;
using ISoftViewerLibrary.Models.Interfaces;
using System.Collections.Generic;

namespace ISoftViewerLibrary.Models.DatabaseTables
{
    #region DicomPatientUniqueIdentifiersTable
    /// <summary>
    /// DicomPatient表格主鍵更新欄位
    /// </summary>
    public class DicomPatientUniqueIdentifiersTable : MasterDetailTable
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userid"></param>
        public DicomPatientUniqueIdentifiersTable(string userid)
            : base(userid, "DicomPatient")
        {
        }

        #region Fields
        /// <summary>
        /// 病歷號碼
        /// </summary>
        public ICommonFieldProperty PatientID { get; private set; }
        /// <summary>
        /// 更新Set用的病歷號碼欄位
        /// </summary>
        public ICommonFieldProperty UpdatePatientID { get; private set; }        
        #endregion        

        #region Methods
        /// <summary>
        /// 指定PatientID欄位資料
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="updatePid"></param>
        /// <returns></returns>
        public DicomPatientUniqueIdentifiersTable SetPatientId(string patientId, string updatePid)
        {
            Apply(new CommandFieldEvent.OnPatientUidCreated()
            {
                PatientID = patientId,
                UpdatedPatientID = updatePid
            });
            return this;
        }
        /// <summary>
        /// 更新病歷號碼
        /// </summary>
        /// <param name="updatePid"></param>
        /// <returns></returns>
        public DicomPatientUniqueIdentifiersTable UpdatePatientId(string updatePid, string modifedUser)
        {
            Apply(new CommandFieldEvent.OnPatientUidUpdated()
            {
                UpdatedPatientID = updatePid,
                ModifiedUser = modifedUser
            });

            return this;
        }
        protected override void When(object @event)
        {
            switch (@event)
            {
                case CommandFieldEvent.OnPatientUidCreated e:
                    PatientID = new TableFieldProperty()
                                .SetDbField("PatientId", FieldType.ftString, isKey: true, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.PatientID, "", null);
                    DBPrimaryKeyFields.Add(PatientID);

                    UpdatePatientID = new TableFieldProperty()
                                .SetDbField("PatientId", FieldType.ftString, isKey: false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.UpdatedPatientID, "", null);
                    DBNormalFields.Add(UpdatePatientID);                    
                    break;
                case CommandFieldEvent.OnPatientUidUpdated e:
                    UpdatePatientID.UpdateDbFieldValues(e.UpdatedPatientID, "", null);                    
                    ModifiedUser.UpdateDbFieldValues(e.ModifiedUser, "", null);                    
                    break;                
                default:
                    base.When(@event);
                    break;
            }
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;

            valid &= PatientID.Value != string.Empty;
            valid &= UpdatePatientID.Value != string.Empty;            

            if (!valid)
                throw new InvalidEntityStateException(this, "Post-checks failed in DicomPatientUniqueIdentifiersTable");
        }
        #endregion
    }
    #endregion

    #region DicomStudyUniqueIdentifiersTable
    /// <summary>
    /// DICOM Study唯一碼表格
    /// </summary>
    public class DicomStudyUniqueIdentifiersTable : MasterDetailTable
    {        
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userid"></param>
        public DicomStudyUniqueIdentifiersTable(string userid)
            : base(userid, "DicomStudy")
        {
            
        }

        #region Fields
        /// <summary>
        /// 病歷號碼
        /// </summary>
        public ICommonFieldProperty PatientID { get; private set; }
        /// <summary>
        /// 檢查唯一碼欄位
        /// </summary>
        public ICommonFieldProperty StudyInstanceUID { get; private set; }
        /// <summary>
        /// 更新主鍵用檢查唯一碼欄位
        /// </summary>
        public ICommonFieldProperty UpdateStudyInstanceUID { get; private set; }
        /// <summary>
        /// 參照檢查唯一碼欄位(更改前)
        /// </summary>
        public ICommonFieldProperty ReferencedStudyInstanceUID { get; private set; }
        #endregion

        #region Methods  
        /// <summary>
        /// 指定PatientID欄位資料
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public DicomStudyUniqueIdentifiersTable SetPatientId(string patientId)
        {
            Apply(new CommandFieldEvent.OnPatientUidCreated()
            {
                PatientID = patientId
            });
            return this;
        }
        /// <summary>
        /// 指定Study Instance UID更新資料
        /// </summary>
        /// <param name="studyUID"></param>
        /// <param name="updateUID"></param>
        /// <param name="refUID"></param>
        /// <returns></returns>
        public DicomStudyUniqueIdentifiersTable SetInstanceUID(string studyUID, string updateUID, string refUID)
        {
            Apply(new CommandFieldEvent.OnStudyUidTableCreated() 
            {
                StudyInstanceUID = studyUID,
                UpdateStudyInstanceUID = updateUID,
                ReferencedStudyInstanceUID = refUID
            });
            return this;
        }
        /// <summary>
        /// 更新Reference Instance UID及Update Instance UID
        /// </summary>
        /// <param name="updateUID"></param>
        /// <param name="refUID"></param>
        /// <param name="modifiedUser"></param>
        /// <returns></returns>
        public DicomStudyUniqueIdentifiersTable UpdateReferenceInstanceUID(string updateUID, string refUID, string modifiedUser)
        {
            Apply(new CommandFieldEvent.OnStudyReferenceUidUpdated()
            {
                UpdateStudyInstanceUID = updateUID,
                ReferencedStudyInstanceUID = refUID,
                ModifiedUser = modifiedUser
            });
            return this;
        }
        /// <summary>
        /// 更新Where Update StudyInstanceUID
        /// </summary>
        /// <param name="updateUID"></param>
        /// <param name="refUID"></param>
        /// <param name="modifiedUser"></param>
        /// <returns></returns>
        public DicomStudyUniqueIdentifiersTable UpdateUpdateInstanceUID(string insUid, string updateUID, string modifyUser)
        {
            Apply(new CommandFieldEvent.OnStudyUidUpdated()
            {
                StudyInstanceUID = insUid,
                UpdateStudyInstanceUID = updateUID,
                ModifiedUser = modifyUser
            });
            return this;
        }
        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {            
            switch (@event)
            {
                case CommandFieldEvent.OnStudyUidTableCreated e:
                    StudyInstanceUID = new TableFieldProperty()
                                .SetDbField("StudyInstanceUID", FieldType.ftString, isKey: true, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.StudyInstanceUID, "", null);
                    DBPrimaryKeyFields.Add(StudyInstanceUID);

                    UpdateStudyInstanceUID = new TableFieldProperty()
                                .SetDbField("StudyInstanceUID", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.UpdateStudyInstanceUID, "", null);
                    DBNormalFields.Add(UpdateStudyInstanceUID);

                    ReferencedStudyInstanceUID = new TableFieldProperty()
                                .SetDbField("ReferencedStudyInstanceUID", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.ReferencedStudyInstanceUID, "", null);
                    DBNormalFields.Add(ReferencedStudyInstanceUID);                    
                    break;
                case CommandFieldEvent.OnStudyReferenceUidUpdated e:
                    UpdateStudyInstanceUID.UpdateDbFieldValues(e.UpdateStudyInstanceUID, "", null);
                    ReferencedStudyInstanceUID.UpdateDbFieldValues(e.ReferencedStudyInstanceUID, "", null);
                    ModifiedUser.UpdateDbFieldValues(e.ModifiedUser, "", null);
                    break;
                case CommandFieldEvent.OnPatientUidCreated e:
                    if (PatientID == null)
                    {
                        PatientID = new TableFieldProperty()
                                .SetDbField("PatientId", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.PatientID, "", null);
                        DBNormalFields.Add(PatientID);
                    }
                    else
                    {
                        PatientID.UpdateDbFieldValues(e.PatientID, "", null);
                    }
                    break;
                case CommandFieldEvent.OnStudyUidUpdated e:
                    StudyInstanceUID.UpdateDbFieldValues(e.StudyInstanceUID, "", null);
                    UpdateStudyInstanceUID.UpdateDbFieldValues(e.UpdateStudyInstanceUID, "", null);
                    break;
                default:
                    base.When(@event);
                    break;
            }
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;

            valid &= StudyInstanceUID.Value != string.Empty;
            valid &= UpdateStudyInstanceUID.Value != string.Empty;
            valid &= ModifiedUser.Value != string.Empty;

            if (!valid)
                throw new InvalidEntityStateException(this, "Post-checks failed in DicomStudyUniqueIdentifiersTable");
        }
        #endregion        
    }
    #endregion

    #region DicomSeriesUniqueIdentifiersTable
    /// <summary>
    /// DICOM Series唯一碼表格
    /// </summary>
    public class DicomSeriesUniqueIdentifiersTable : MasterDetailTable
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="userid"></param>
        public DicomSeriesUniqueIdentifiersTable(string userid)
            : base(userid, "DicomSeries")
        {            
        }

        #region Fields
        /// <summary>
        /// 系列唯一碼欄位
        /// </summary>
        public ICommonFieldProperty SeriesInstanceUID { get; private set; }
        /// <summary>
        /// 更新主鍵用系列唯一碼欄位
        /// </summary>
        public ICommonFieldProperty UpdateSeriesInstanceUID { get; private set; }
        /// <summary>
        /// 檢查唯一碼欄位
        /// </summary>
        public ICommonFieldProperty StudyInstanceUID { get; private set; }
        /// <summary>
        /// 參照檢查唯一碼欄位(更改後)
        /// </summary>
        public ICommonFieldProperty ReferencedStudyInstanceUID { get; private set; }
        /// <summary>
        /// 參照系列唯一碼欄位(更改後)
        /// </summary>
        public ICommonFieldProperty ReferencedSeriesInstanceUID { get; private set; }
        /// <summary>
        /// 目前事件狀態
        /// </summary>
        protected SeriesUidTableState EventState;
        #endregion

        #region Methods
        /// <summary>
        /// 指定Study & Series InstanceUID資料
        /// </summary>
        /// <param name="seriesUID"></param>
        /// <param name="updateUID"></param>
        /// <param name="studyUID"></param>
        /// <param name="refStudyUID"></param>
        /// <param name="refSeriesUID"></param>
        /// <returns></returns>
        public DicomSeriesUniqueIdentifiersTable SetInstanceUID(string seriesUID, string updateUID, string studyUID, string refStudyUID, 
            string refSeriesUID)
        {
            Apply(new CommandFieldEvent.OnSeriesUidCreated() 
            {
                SeriesInstanceUID = seriesUID,
                UpdateSeriesInstanceUID = updateUID,
                StudyInstanceUID = studyUID,
                ReferencedStudyInstanceUID = refStudyUID,
                ReferencedSeriesInstanceUID = refSeriesUID
            });
            return this;
        }
        /// <summary>
        /// 更新InstanceUID資料
        /// </summary>
        /// <param name="updateUID"></param>
        /// <param name="studyUID"></param>
        /// <param name="modifierUser"></param>
        /// <returns></returns>
        public DicomSeriesUniqueIdentifiersTable UpdateInstanceUIDAndData(string updateUID, string studyUID, string modifierUser)
        {
            Apply(new CommandFieldEvent.OnSeriesUidUpdated() 
            {
                UpdateSeriesInstanceUID = updateUID,
                StudyInstanceUID = studyUID,
                ModifiedUser = modifierUser
            });
            return this;
        }
        /// <summary>
        /// 更新參考Reference Instance UID資料
        /// </summary>
        /// <param name="refSeUid"></param>
        /// <param name="refStUid"></param>
        /// <returns></returns>
        public DicomSeriesUniqueIdentifiersTable UpdateReferenceInstanceUID(string refSeUid, string refStUid)
        {
            Apply(new CommandFieldEvent.OnSeriesReferenceUidUpdated() 
            {
                ReferencedSeriesInstanceUID = refSeUid,
                ReferencedStudyInstanceUID = refStUid
            });
            return this;
        }
        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {
            switch (@event)
            {
                case CommandFieldEvent.OnSeriesUidCreated e:
                    StudyInstanceUID = new TableFieldProperty()
                                .SetDbField("StudyInstanceUID", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.StudyInstanceUID, "", null);
                    DBPrimaryKeyFields.Add(StudyInstanceUID);

                    SeriesInstanceUID = new TableFieldProperty()
                                .SetDbField("SeriesInstanceUID", FieldType.ftString, isKey: true, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.SeriesInstanceUID, "", null);
                    DBNormalFields.Add(SeriesInstanceUID);

                    UpdateSeriesInstanceUID = new TableFieldProperty()
                                .SetDbField("SeriesInstanceUID", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.UpdateSeriesInstanceUID, "", null);
                    DBNormalFields.Add(UpdateSeriesInstanceUID);

                    ReferencedStudyInstanceUID = new TableFieldProperty()
                                .SetDbField("ReferencedStudyInstanceUID", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.ReferencedStudyInstanceUID, "", null);
                    DBNormalFields.Add(ReferencedStudyInstanceUID);

                    ReferencedSeriesInstanceUID = new TableFieldProperty()
                                .SetDbField("ReferencedSeriesInstanceUID", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.ReferencedSeriesInstanceUID, "", null);
                    DBNormalFields.Add(ReferencedStudyInstanceUID);                    
                    EventState = SeriesUidTableState.seCreate;
                    break;
                case CommandFieldEvent.OnSeriesUidUpdated e:
                    UpdateSeriesInstanceUID.UpdateDbFieldValues(e.UpdateSeriesInstanceUID, "", null);
                    StudyInstanceUID.UpdateDbFieldValues(e.StudyInstanceUID, "", null);
                    ModifiedUser.UpdateDbFieldValues(e.ModifiedUser, "", null);
                    EventState = SeriesUidTableState.seUpdateUidAndData;
                    break;
                case CommandFieldEvent.OnSeriesReferenceUidUpdated e:
                    if (ReferencedStudyInstanceUID.Value == "")
                        ReferencedStudyInstanceUID.UpdateDbFieldValues(e.ReferencedStudyInstanceUID, "", null);
                    if (ReferencedSeriesInstanceUID.Value == "")
                        ReferencedSeriesInstanceUID.UpdateDbFieldValues(e.ReferencedSeriesInstanceUID, "", null);
                    EventState = SeriesUidTableState.seUpdateRefUID;
                    break;
                default:
                    base.When(@event);
                    break;
            }
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;
            switch(EventState)
            {
                case SeriesUidTableState.seCreate:
                    valid &= StudyInstanceUID.Value != string.Empty;
                    valid &= SeriesInstanceUID.Value != string.Empty;
                    valid &= UpdateSeriesInstanceUID.Value != string.Empty;
                    valid &= ModifiedUser.Value != string.Empty;
                    break;
                case SeriesUidTableState.seUpdateUidAndData:
                    valid &= StudyInstanceUID.Value != string.Empty;
                    valid &= UpdateSeriesInstanceUID.Value != string.Empty;
                    break;
            }            

            if (!valid)
                throw new InvalidEntityStateException(this, "Post-checks failed in DicomSeriesUniqueIdentifiersTable");
        }
        /// <summary>
        /// 更新欄位對調
        /// </summary>
        public void UpdateKeyValueSwap()
        {
            DBPrimaryKeyFields.Clear();
            DBNormalFields.Clear();

            DBPrimaryKeyFields.Add(SeriesInstanceUID);

            DBNormalFields.Add(StudyInstanceUID);
            DBNormalFields.Add(UpdateSeriesInstanceUID);
            DBNormalFields.Add(ReferencedStudyInstanceUID);
            DBNormalFields.Add(ReferencedSeriesInstanceUID);

            DBNormalFields.Add(CreateUser);
            DBNormalFields.Add(CreateDateTime);
            DBNormalFields.Add(ModifiedUser);
            DBNormalFields.Add(ModifiedDateTime);
        }
        #endregion

        #region enum
        /// <summary>
        /// 物件動作列舉值
        /// </summary>
        protected enum SeriesUidTableState { seCreate, seUpdateRefUID, seUpdateUidAndData };
        #endregion
    }
    #endregion

    #region DicomImageUniqueIdentifiersTable
    /// <summary>
    ///  DICOM Image唯一碼表格
    /// </summary>
    public class DicomImageUniqueIdentifiersTable : ElementAbstract
    {
        public DicomImageUniqueIdentifiersTable(string userId)
            : base(userId)
        {
            TableName = "DicomImage";            
        }

        #region Fields
        /// <summary>
        /// 影像唯一碼
        /// </summary>
        public ICommonFieldProperty SOPInstanceUID { get; private set; }
        /// <summary>
        /// 更新主鍵影像唯一碼
        /// </summary>
        public ICommonFieldProperty UpdateSOPInstanceUID { get; private set; }
        /// <summary>
        /// 系列唯一碼
        /// </summary>
        public ICommonFieldProperty SeriesInstanceUID { get; private set; }
        /// <summary>
        /// 參照影像唯一碼欄位(更改後)
        /// </summary>
        public ICommonFieldProperty ReferencedSOPInstanceUID { get; private set; }
        /// <summary>
        /// 參照系列唯一碼欄位(更改後)
        /// </summary>
        public ICommonFieldProperty ReferencedSeriesInstanceUID { get; private set; }
        /// <summary>
        /// 儲存裝置的裝置號碼
        /// </summary>
        public ICommonFieldProperty StorageDeviceID { get; private set; }
        /// <summary>
        /// 檔案路徑
        /// </summary>
        public ICommonFieldProperty FilePath { get; private set; }
        /// <summary>
        /// 未更改前的檢查及系列資料
        /// </summary>
        public ICommonFieldProperty UnmappedDcmTag { get; private set; }
        /// <summary>
        /// 目前事件狀態
        /// </summary>
        protected ImageUidTableState EventState;
        #endregion

        #region Methods
        /// <summary>
        /// 指定InstanceUID
        /// </summary>
        /// <param name="sopUID"></param>
        /// <param name="updateUID"></param>
        /// <param name="seriesUID"></param>
        /// <param name="storageDev"></param>
        /// <param name="filePath"></param>
        /// <param name="refSeriesUID"></param>
        /// <param name="refSopUID"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable SetInstanceUID(string sopUID, string updateUID, string seriesUID, 
            string storageDev, string filePath, string refSeriesUID, string refSopUID, string umappedTagData)
        {           
            Apply(new CommandFieldEvent.OnImageUidCreated()
            {
                SOPInstanceUID = sopUID,
                UpdateSOPInstanceUID = updateUID,
                SeriesInstanceUID = seriesUID,
                ReferencedSOPInstanceUID = refSopUID,
                ReferencedSeriesInstanceUID = refSeriesUID,
                StorageDeviceID = storageDev,
                FilePath = filePath,
                UnmappedDcmTags = umappedTagData
            });
            return this;
        }
        /// <summary>
        /// 更新檔案路徑
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable UpdateFilePath(string path)
        {
            Apply(new CommandFieldEvent.OnImageFilePathUpdated()
            {
                FilePath = path
            });
            return this;
        }
        /// <summary>
        /// 更新ReferencedSOPInstanceUID & ReferencedSeriesInstanceUID
        /// </summary>
        /// <param name="sopUid"></param>
        /// <param name="serUid"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable UpdateReferenceUID(string sopUid, string serUid)
        {
            Apply(new CommandFieldEvent.OnImageReferenceUidUpdated() 
            {
                ReferencedSOPInstanceUID = sopUid,
                ReferencedSeriesInstanceUID = serUid
            });
            return this;
        }
        /// <summary>
        /// 更新Instance UID和路徑資料
        /// </summary>
        /// <param name="seUid"></param>
        /// <param name="updateSopUid"></param>
        /// <param name="path"></param>
        /// <param name="modDate"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable UpdateInstanceUIDAndPath(string seUid, string updateSopUid, string path, string modDate)
        {
            Apply(new CommandFieldEvent.OnImageUidAndFilePathUpdated() 
            {
                SeriesInstanceUID = seUid,
                UpdateSOPInstanceUID = updateSopUid,
                FilePath = path,
                ModifiedUser = modDate
            });
            return this;
        }
        /// <summary>
        /// 更新Instance UID和路徑資料
        /// </summary>
        /// <param name="seUid"></param>
        /// <param name="updateSopUid"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable UpdateInstanceUID(string seUid, string updateSopUid)
        {
            Apply(new CommandFieldEvent.OnImageUidUpdated() 
            {
                SeriesInstanceUID = seUid,
                UpdateSOPInstanceUID = updateSopUid,
            });
            return this;
        }
        /// <summary>
        /// 更新,把原始資保留下來
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public DicomImageUniqueIdentifiersTable UpdateUnmappedDcmTag(string data)
        {
            Apply(new CommandFieldEvent.OnImageUnmappedDcmTagUpdated()
            {
                UnmappedDcmTags = data
            });
            return this;
        }
        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {
            switch (@event)
            {
                case CommandFieldEvent.OnImageUidCreated e:
                    SeriesInstanceUID = new TableFieldProperty()
                                .SetDbField("SeriesInstanceUID", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                .UpdateDbFieldValues(e.SeriesInstanceUID, "", null);
                    DBPrimaryKeyFields.Add(SeriesInstanceUID);

                    SOPInstanceUID = new TableFieldProperty()
                                 .SetDbField("SOPInstanceUID", FieldType.ftString, isKey: true, false, true, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.SOPInstanceUID, "", null);
                    DBNormalFields.Add(SOPInstanceUID);

                    UpdateSOPInstanceUID = new TableFieldProperty()
                                 .SetDbField("SOPInstanceUID", FieldType.ftString, isKey: true, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.UpdateSOPInstanceUID, "", null);
                    DBNormalFields.Add(UpdateSOPInstanceUID);

                    StorageDeviceID = new TableFieldProperty()
                                 .SetDbField("StorageDeviceID", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.StorageDeviceID, "", null);
                    DBNormalFields.Add(StorageDeviceID);

                    FilePath = new TableFieldProperty()
                                 .SetDbField("FilePath", FieldType.ftString, false, false, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.FilePath, "", null);
                    DBNormalFields.Add(FilePath);

                    ReferencedSOPInstanceUID = new TableFieldProperty()
                                 .SetDbField("ReferencedSOPInstanceUID", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.ReferencedSOPInstanceUID, "", null);
                    DBNormalFields.Add(ReferencedSOPInstanceUID);

                    ReferencedSeriesInstanceUID = new TableFieldProperty()
                                 .SetDbField("ReferencedSeriesInstanceUID", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.ReferencedSeriesInstanceUID, "", null);
                    DBNormalFields.Add(ReferencedSeriesInstanceUID);

                    UnmappedDcmTag = new TableFieldProperty()
                                 .SetDbField("UnmappedDcmTags", FieldType.ftString, false, true, false, false, FieldOperator.foAnd, OrderOperator.foNone)
                                 .UpdateDbFieldValues(e.UnmappedDcmTags, "", null);
                    EventState = ImageUidTableState.imCreate;
                    break;
                case CommandFieldEvent.OnImageFilePathUpdated e:
                    FilePath.UpdateDbFieldValues(e.FilePath, "", null);
                    EventState = ImageUidTableState.imUpdateFilePath;
                    break;
                case CommandFieldEvent.OnImageReferenceUidUpdated e:
                    if (ReferencedSOPInstanceUID.Value == "")
                        ReferencedSOPInstanceUID.UpdateDbFieldValues(e.ReferencedSOPInstanceUID, "", null);
                    if (ReferencedSeriesInstanceUID.Value == "")
                        ReferencedSeriesInstanceUID.UpdateDbFieldValues(e.ReferencedSeriesInstanceUID, "", null);
                    EventState = ImageUidTableState.imUpdateRefUID;
                    break;
                case CommandFieldEvent.OnImageUidAndFilePathUpdated e:
                    SeriesInstanceUID.UpdateDbFieldValues(e.SeriesInstanceUID, "", null);
                    UpdateSOPInstanceUID.UpdateDbFieldValues(e.UpdateSOPInstanceUID, "", null);
                    FilePath.UpdateDbFieldValues(e.FilePath, "", null);
                    ModifiedUser.UpdateDbFieldValues(e.ModifiedUser, "", null);
                    EventState = ImageUidTableState.imUpdateUidAndPath;
                    break;
                case CommandFieldEvent.OnImageUidUpdated e:
                    SeriesInstanceUID.UpdateDbFieldValues(e.SeriesInstanceUID, "", null);
                    UpdateSOPInstanceUID.UpdateDbFieldValues(e.UpdateSOPInstanceUID, "", null);
                    break;
                case CommandFieldEvent.OnImageUnmappedDcmTagUpdated e:
                    UnmappedDcmTag.UpdateDbFieldValues(e.UnmappedDcmTags, "", null);
                    EventState = ImageUidTableState.imUpdateMappedField;
                    break;                
                default:
                    break;
            }
        }
        /// <summary>
        /// 確認資料是否正確
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;
            switch (EventState)
            {
                case ImageUidTableState.imCreate:
                    valid &= SeriesInstanceUID.Value != string.Empty;
                    valid &= SOPInstanceUID.Value != string.Empty;
                    valid &= UpdateSOPInstanceUID.Value != string.Empty;
                    valid &= ModifiedUser.Value != string.Empty;
                    valid &= FilePath.Value != string.Empty;
                    break;
                case ImageUidTableState.imUpdateFilePath:
                    valid &= FilePath.Value != string.Empty;
                    break;
                case ImageUidTableState.imUpdateRefUID:
                    break;  //Not thinsg
                case ImageUidTableState.imUpdateUidAndPath:
                    valid &= SeriesInstanceUID.Value != string.Empty;
                    valid &= UpdateSOPInstanceUID.Value != string.Empty;
                    valid &= ModifiedUser.Value != string.Empty;
                    valid &= FilePath.Value != string.Empty;
                    break;
            }            

            if (!valid)
                throw new InvalidEntityStateException(this, "Post-checks failed in DicomImageUniqueIdentifiersTable");
        }
        /// <summary>
        /// 更新欄位對調
        /// </summary>
        public void UpdateKeyValueSwap()
        {
            DBPrimaryKeyFields.Clear();
            DBNormalFields.Clear();

            DBPrimaryKeyFields.Add(SOPInstanceUID);

            DBNormalFields.Add(SeriesInstanceUID);
            DBNormalFields.Add(UpdateSOPInstanceUID);
            DBNormalFields.Add(ReferencedSOPInstanceUID);
            DBNormalFields.Add(ReferencedSeriesInstanceUID);
            DBNormalFields.Add(StorageDeviceID);
            DBNormalFields.Add(FilePath);
            DBNormalFields.Add(UnmappedDcmTag);

            DBNormalFields.Add(CreateUser);
            DBNormalFields.Add(CreateDateTime);
            DBNormalFields.Add(ModifiedUser);
            DBNormalFields.Add(ModifiedDateTime);
        }
        #endregion

        #region enum
        /// <summary>
        /// 物件動作列舉值
        /// </summary>
        protected enum ImageUidTableState { imCreate, imUpdateFilePath, imUpdateRefUID, imUpdateUidAndPath, imUpdateMappedField };
        #endregion
    }
    #endregion
}
