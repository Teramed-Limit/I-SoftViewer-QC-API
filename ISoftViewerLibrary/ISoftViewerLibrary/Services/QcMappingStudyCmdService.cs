using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Logics.QCOperation.Modal;
using ISoftViewerLibrary.Models.Events;

namespace ISoftViewerLibrary.Services
{
    #region QcMappingStudyCmdService

    /// <summary>
    /// 檢查批配對應的Dicom Dataset資料
    /// </summary>
    public class QcMappingStudyCmdService : QcStudyCmdWithDcmNetService<DataCorrection.V1.StudyMappingParameter>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        /// <param name="dcmUnitOfWork"></param>
        /// <param name="dcmCqusDatasts"></param>
        /// <param name="mappingTabJson"></param>
        public QcMappingStudyCmdService(DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService,
            IDcmUnitOfWork dcmUnitOfWork, IDcmCqusDatasets dcmCqusDatasts, EnvironmentConfiguration publicConfig)
            : base(dbQryService, dbCmdService, dcmUnitOfWork, dcmCqusDatasts, publicConfig)
        {
            //取得DICOM Tag對應資料
            MappingTagDataset = publicConfig.DcmTagMappingTable;
            NewPatientID = "";
            NewStudyInstanceUID = "";
        }

        #region Fields

        /// <summary>
        /// 註冊資料
        /// </summary>
        protected DataCorrection.V1.StudyMappingParameter Data;

        /// <summary>
        /// DICOM tag mapping table字串
        /// </summary>
        private readonly MappingTagTable MappingTagDataset;

        /// <summary>
        /// QC操作紀錄
        /// </summary>
        private QCOperationContext OperationContext { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 註冊資料
        /// </summary>
        /// <param name="data"></param>
        public override void RegistrationData(object data)
        {
            Data = (DataCorrection.V1.StudyMappingParameter)data;
            //先取得PatientID & StudyInstanceUID
            if (Data != null)
            {
                Data.Dataset.ForEach(data =>
                {
                    if (data.Group == DicomTag.PatientID.Group && data.Elem == DicomTag.PatientID.Element)
                        NewPatientID = data.Value;
                    if (data.Group == DicomTag.StudyInstanceUID.Group && data.Elem == DicomTag.StudyInstanceUID.Element)
                        NewStudyInstanceUID = data.Value;
                });
            }

            OriginalPatientID = string.Empty;
            OriginalStudyInstanceUID = string.Empty;
        }

        /// <summary>
        /// 註冊Study操作資料
        /// </summary>
        /// <param name="operationContext"></param>
        public override void RegistrationOperationContext(QCOperationContext operationContext)
        {
            OperationContext = operationContext;
            OperationContext.SetLogger(new MappingStudyLogger());
        }

        /// <summary>
        /// 執行動作
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Execute()
        {
            try
            {
                Messages.Add("    *** Start mapping-study data and image ***");
                Messages.Add("     ** Mapping Study Instance UID : " + Data.StudyInstanceUID);

                //先確定Study是否已經存在
                if (await CheckStudyInsUidIsExist())
                    throw new Exception("Study Instance UID duplicated.");

                //取得要做Mapping的檢查,系列及影像
                if (await QueryUidTable(Data.ModifyUser, Data.StudyInstanceUID) == false ||
                    await QueryStorageDevice() == false)
                    throw new Exception("Failed to query to mapping table or storage device data!!");

                //判斷是否更新資料庫,如果有PatientID及StudyInstanceUID,代表資料庫需要先更新
                OriginalPatientID = TobeDcmStudyUidTable.PatientID.Value.Trim();
                OriginalStudyInstanceUID = TobeDcmStudyUidTable.StudyInstanceUID.Value.Trim();
                //判斷NewPatientID和NewStudyInstanceUID是否有值,若沒有資料,要補上目前的資料
                if (NewPatientID == string.Empty)
                    NewPatientID = TobeDcmStudyUidTable.PatientID.Value.Trim();
                if (NewStudyInstanceUID == string.Empty)
                    NewStudyInstanceUID = TobeDcmStudyUidTable.StudyInstanceUID.Value.Trim();

                bool needToUpdateDb =
                    (OriginalPatientID != NewPatientID || OriginalStudyInstanceUID != NewStudyInstanceUID);

                Dictionary<string, DicomFile> modifiedDcmFile = new();
                DicomOperatorHelper dcmHelper = new();
                bool haveProcessed = true;

                for (int seIdx = 0; seIdx < TobeDcmStudyUidTable.DetailElements.Count; seIdx++)
                {
                    var mergedIndexList = new List<int>();

                    if (TobeDcmStudyUidTable.DetailElements[seIdx] is not DicomSeriesUniqueIdentifiersTable _seTable)
                        throw new Exception("        Illegal series table");

                    //Series Table關聯要先處理
                    string seriesUID = _seTable.SeriesInstanceUID.Value.Trim();
                    _seTable.UpdateInstanceUIDAndData(seriesUID, NewStudyInstanceUID, Data.ModifyUser);
                    _seTable.UpdateKeyValueSwap();
                    //Mapping不會動到Image層的資料,所以資料庫不需要更新任何資料
                    for (int imIdx = 0; imIdx < _seTable.DetailElements.Count; imIdx++)
                    {
                        //先組合完整的檔案路徑
                        if (_seTable.DetailElements[imIdx] is not DicomImageUniqueIdentifiersTable _imgTable)
                            throw new Exception("        Illegal image table");

                        //被Merged的不要記錄到資料庫
                        if (_imgTable.ReferencedSeriesInstanceUID.Value != string.Empty)
                            mergedIndexList.Add(imIdx);

                        string dcmFilePath = string.Empty;
                        DicomFile dcmFile = GetDicomFile(_imgTable, dcmHelper, ref dcmFilePath);
                        //Mapping資料,只要有檔案不需要調整,則不繼續處理
                        if ((haveProcessed &= MappingDatasetToDcmFile(dcmFile, dcmHelper)) == false)
                            break;
                        modifiedDcmFile.Add(dcmFilePath, dcmFile);
                        _imgTable.UpdateKeyValueSwap();
                    }

                    mergedIndexList.Reverse();
                    foreach (var skipImgIdx in mergedIndexList)
                    {
                        _seTable.DetailElements.RemoveAt(skipImgIdx);
                    }
                }

                OperationContext.SetParams(Data.ModifyUser, Data.StudyInstanceUID, "", "");

                //如果資料有異動才做處理
                if (haveProcessed == true)
                {
                    //先更新資料庫
                    if (needToUpdateDb == true)
                        await UpdateDicomTableToDatabase();
                    //上傳Teramed PACS Service
                    if (await SendDcmToScp(modifiedDcmFile) == false)
                        throw new Exception(Message);
                    //更新狀態
                    await UpdateStudyMaintainStatusToDatabase(NewStudyInstanceUID);
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                OperationContext.WriteFailedRecord();
                PrintLog();
                throw new Exception(Message);
            }

            Result = OpResult.OpSuccess;
            OperationContext.WriteSuccessRecord();
            Messages.Add("    *** Successful mapping study data and image");
            return true;
        }

        /// <summary>
        /// 依據Table內容去取得DicomFile物件
        /// </summary>
        /// <param name="imgUidTable"></param>
        /// <returns></returns>
        protected override DicomFile GetDicomFile(DicomImageUniqueIdentifiersTable imgUidTable,
            DicomOperatorHelper dcmHelper,
            ref string dcmFilePath)
        {
            if (imgUidTable.FilePath.Value == "")
                throw new Exception("        Illegal FilePath field");
            if (DeviceIdMappingTable.ContainsKey(imgUidTable.StorageDeviceID.Value) == false)
                throw new Exception("        Illegal StorageDeviceID field");

            //現有檔案路徑
            string storagePath = DeviceIdMappingTable[imgUidTable.StorageDeviceID.Value].Trim();
            dcmFilePath = storagePath + imgUidTable.FilePath.Value.Trim();
            //尚未支援壓縮檔案格式處理
            DicomFile dcmFile = DicomFile.Open(dcmFilePath);
            if (dcmFile == null)
                throw new Exception("        Can not open file : " + dcmFilePath);
            //要先備份Mapping之前的資料
            if (imgUidTable.UnmappedDcmTag.Value.Trim() == string.Empty)
            {
                string unmappedTagTable = GetUnmappedDcmTags(dcmFile, MappingTagDataset, dcmHelper);
                imgUidTable.UpdateUnmappedDcmTag(unmappedTagTable);
            }

            return dcmFile;
        }

        /// <summary>
        /// 取得未更改之前的
        /// </summary>
        /// <param name="dicomFile"></param>
        /// <param name="tagTable"></param>
        /// <returns></returns>
        private string GetUnmappedDcmTags(DicomFile dicomFile, MappingTagTable tagTable, DicomOperatorHelper dcmHelper)
        {
            string result = string.Empty;
            try
            {
                DicomDataset dataset = dicomFile.Dataset;
                //先判斷是什麼編碼
                string value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset,
                    DicomTag.SpecificCharacterSet.Group,
                    DicomTag.SpecificCharacterSet.Element, false);
                bool isUtf8 = value.Contains("192");

                List<DataCorrection.V1.DcmTagData> dcmTagDatas = new();
                tagTable.Dataset.ForEach(data =>
                {
                    //取得Tag編號
                    dcmHelper.ConvertTagStringToUIntGE(data.ToTag, out ushort group, out ushort element);
                    // 若有值，則取值，否則取原始值
                    // value = !string.IsNullOrEmpty(data.Value)
                    //     ? data.Value
                    //     : 
                    value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, group, element, isUtf8);
                    dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, group, element, isUtf8);

                    dcmTagDatas.Add(new DataCorrection.V1.DcmTagData()
                        { Group = group, Elem = element, Value = value });
                });

                if (dcmTagDatas.Any() == true)
                    result = JsonSerializer.Serialize(dcmTagDatas);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                throw new Exception(Message);
            }

            return result;
        }

        /// <summary>
        /// 將外部資料更新到DICOM檔案
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        private bool MappingDatasetToDcmFile(DicomFile dcmFile, DicomOperatorHelper dcmHelper)
        {
            bool result = false;
            try
            {
                DicomDataset dataset = dcmFile.Dataset;
                string value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset,
                    DicomTag.SpecificCharacterSet.Group,
                    DicomTag.SpecificCharacterSet.Element, false);
                bool isUtf8 = value.Contains("192");

                MappingTagDataset.Dataset.ForEach(mapping_dataset =>
                {
                    dcmHelper.ConvertTagStringToUIntGE(mapping_dataset.FromTag, out ushort f_group,
                        out ushort f_element);
                    DataCorrection.V1.DcmTagData dtagData =
                        Data.Dataset.Find(tag => tag.Group == f_group && tag.Elem == f_element);

                    if (dtagData != null)
                    {
                        dcmHelper.ConvertTagStringToUIntGE(mapping_dataset.ToTag, out ushort t_group,
                            out ushort t_element);
                        // 從原始Dicom檔案取得值，要判斷SpecificCharacterSet來決定用什麼編碼解析
                        string originalValue =
                            dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, t_group, t_element, isUtf8);
                        if (originalValue != dtagData.Value)
                        {
                            // 拿到的中文字一律一定都是Utf8
                            dcmHelper.WriteDicomValueInDataset(dataset, new DicomTag(t_group, t_element), dtagData.Value, isUtf8);
                            result = true;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                throw new Exception(Message);
            }

            return result;
        }

        /// <summary>
        /// 更新檢查層Table
        /// </summary>
        /// <returns></returns>
        protected override async Task UpdateDicomTableToDatabase()
        {
            if (NeedUpdatePatientTable == true)
                DcmPatientUidTable.UpdatePatientId(NewPatientID, Data.ModifyUser);

            string originalUid = TobeDcmStudyUidTable.StudyInstanceUID.Value.Trim();
            TobeDcmStudyUidTable
                .UpdateUpdateInstanceUID(insUid: originalUid, updateUID: NewStudyInstanceUID, Data.ModifyUser)
                .SetPatientId(NewPatientID);

            await base.UpdateDicomTableToDatabase();
        }

        /// <summary>
        /// 更新Study QC操作狀態
        /// </summary>
        /// <returns></returns>
        protected async Task UpdateStudyMaintainStatusToDatabase(string studyInstanceUID)
        {
            DcmStudyQCStatusTable = new DicomStudyQCStatusTable("");
            DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(studyInstanceUID,
                CommandFieldEvent.StudyMaintainType.Mapped, 1);
            DbCmdService.TableElement = DcmStudyQCStatusTable;
            bool result = await DbCmdService.AddOrUpdate(true);
        }

        /// <summary>
        /// 確定PatientId和StudyInsUid沒有重複才進行Mapping
        /// </summary>
        /// <returns></returns>
        protected Task<bool> CheckStudyInsUidIsExist()
        {
            var studyInstanceUID = Data.Dataset.First(x =>
                x.Group == DicomTag.StudyInstanceUID.Group && x.Elem == DicomTag.StudyInstanceUID.Element).Value;

            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = studyInstanceUID } },
            };

            var table = DbQryService.BuildTable("DcmFindStudyLevelView", pkeys, new List<PairDatas>()).GetData();

            return Task.FromResult(table.DBDatasets.Any());
        }

        #endregion
    }

    #endregion
}