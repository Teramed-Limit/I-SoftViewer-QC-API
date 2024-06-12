using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Models.Events;

namespace ISoftViewerLibrary.Services
{
    #region QcUnmappingStudyCdmService

    /// <summary>
    /// 檢查反批配回覆病患檢查資料
    /// </summary>
    public class QcUnmappingStudyCdmService : QcStudyCmdWithDcmNetService<DataCorrection.V1.StudyUnmappingParameter>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        /// <param name="dcmUnitOfWork"></param>
        /// <param name="dcmCqusDatasts"></param>
        /// <param name="publicConfig"></param>
        /// <param name="svrConfiguration"></param>
        public QcUnmappingStudyCdmService(DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService,
            IDcmUnitOfWork dcmUnitOfWork, IDcmCqusDatasets dcmCqusDatasts, EnvironmentConfiguration publicConfig,
            IEnumerable<SvrConfigurationsV2> svrConfiguration)
            : base(dbQryService, dbCmdService, dcmUnitOfWork, dcmCqusDatasts, publicConfig, svrConfiguration)
        {
        }

        #region Fields

        /// <summary>
        /// 註冊資料
        /// </summary>
        protected DataCorrection.V1.StudyUnmappingParameter Data;

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
        public override async Task RegistrationData(object data)
        {
            Data = (DataCorrection.V1.StudyUnmappingParameter)data;
        }

        /// <summary>
        /// 註冊Study操作資料
        /// </summary>
        /// <param name="operationContext"></param>
        public override void RegistrationOperationContext(QCOperationContext operationContext)
        {
            OperationContext = operationContext;
            OperationContext.SetLogger(new UnMappingStudyLogger());
        }

        /// <summary>
        /// 執行動作
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Execute()
        {
            try
            {
                Serilog.Log.Information("Start unmapping-study data and image");
                Serilog.Log.Information("Unmapping Study Instance UID : " + Data.StudyInstanceUID);

                // 獲取本地C-Store Node
                QueryLocalCStoreNode();

                //先確認是否DICOM
                //取得要做Mapping的檢查,系列及影像
                if (await QueryUidTable(Data.ModifyUser, Data.StudyInstanceUID) == false ||
                    await QueryStorageDevice() == false)
                    throw new Exception("Failed to query to mapping table or storage device data!!");

                Dictionary<string, DicomFile> modifiedDcmFile = new();
                DicomOperatorHelper dcmHelper = new();
                //判斷是否更新資料庫,如果有PatientID及StudyInstanceUID,代表資料庫需要先更新                
                bool needToUpdateDb = false;
                bool haveProcessed = false;


                string newSeriesUID = "";
                string newSopInstanceUID = "";
                for (int seIdx = 0; seIdx < TobeDcmStudyUidTable.DetailElements.Count; seIdx++)
                {
                    // var unmappedIndexList = new List<int>();
                    if (TobeDcmStudyUidTable.DetailElements[seIdx] is not DicomSeriesUniqueIdentifiersTable _seTable)
                        throw new Exception("Illegal series table");

                    var beenMerged = _seTable.ReferencedStudyInstanceUID.Value != string.Empty;

                    for (int imIdx = 0; imIdx < _seTable.DetailElements.Count; imIdx++)
                    {
                        //先組合完整的檔案路徑
                        if (_seTable.DetailElements[imIdx] is not DicomImageUniqueIdentifiersTable _imgTable)
                            throw new Exception("Illegal image table");

                        //被合併過且有Mapping的Study必須先解除合併
                        if (beenMerged)
                            throw new Exception("Please split study first");

                        //欄位沒有值,則不需要處理
                        if (_imgTable.UnmappedDcmTag.Value == string.Empty)
                        {
                            continue;
                        }
                        
                        List<DataCorrection.V1.DcmTagData> dcmTagDatas;
                        dcmTagDatas =
                            JsonSerializer.Deserialize<List<DataCorrection.V1.DcmTagData>>(_imgTable.UnmappedDcmTag
                                .Value);

                        string dcmFilePath = string.Empty;
                        DicomFile dcmFile = GetDicomFile(_imgTable, dcmHelper, ref dcmFilePath);

                        //取得被Mapping的PatientID & StudyInstanceUID
                        if (OriginalPatientID == string.Empty || OriginalStudyInstanceUID == string.Empty)
                        {
                            DataCorrection.V1.DcmTagData data;
                            if ((data = dcmTagDatas.Find(x =>
                                    x.Group == DicomTag.PatientID.Group && x.Elem == DicomTag.PatientID.Element)) !=
                                null)
                                OriginalPatientID = data.Value;

                            if ((data = dcmTagDatas.Find(x =>
                                    x.Group == DicomTag.StudyInstanceUID.Group &&
                                    x.Elem == DicomTag.StudyInstanceUID.Element)) != null)
                                OriginalStudyInstanceUID = data.Value;
                            //同一筆檢查的資料更新,不需要Update資料庫
                            needToUpdateDb = (OriginalPatientID != string.Empty ||
                                              OriginalStudyInstanceUID != string.Empty);

                            if (OriginalPatientID == string.Empty)
                                OriginalPatientID = TobeDcmStudyUidTable.PatientID.Value.Trim();
                            if (OriginalStudyInstanceUID == string.Empty)
                                OriginalStudyInstanceUID = TobeDcmStudyUidTable.StudyInstanceUID.Value;
                        }

                        //Unmapping資料
                        UnmappingDatasetToDcmFile(dcmTagDatas, dcmFile, dcmHelper);
                        modifiedDcmFile.Add(dcmFilePath, dcmFile);
                        
                        // 更新Instance UID
                        newSeriesUID = dcmFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID);
                        newSopInstanceUID = dcmFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID);
                        
                        //清除Mapping記錄欄位
                        _imgTable.UpdateInstanceUID(newSeriesUID, newSopInstanceUID);
                        _imgTable.UpdateUnmappedDcmTag("");
                        _imgTable.UpdateKeyValueSwap();
                        haveProcessed = true;
                    }

                    if (haveProcessed == true)
                    {
                        _seTable.UpdateInstanceUIDAndData(updateUID: newSeriesUID, studyUID: OriginalStudyInstanceUID, Data.ModifyUser);
                        _seTable.UpdateKeyValueSwap();
                    }
                }

                //如果資料有異動才做處理
                if (haveProcessed == true)
                {
                    //先更新資料庫
                    if (needToUpdateDb == true)
                        await UpdateDicomTableToDatabase();
                    //更新狀態
                    await UpdateStudyMaintainStatusToDatabase(OriginalStudyInstanceUID);
                    //QC operation log
                    OperationContext.SetParams(Data.ModifyUser, OriginalStudyInstanceUID);
                    //上傳Teramed PACS Service
                    if (await SendDcmToScp(modifiedDcmFile) == false)
                        throw new Exception(Message);
                }
                else
                {
                    throw new Exception("No mapping required for this check");
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                // OperationContext.WriteFailedRecord(ex.Message, ex.ToString());
                Serilog.Log.Error(ex, "Unmapping study data and image failed");
                throw new Exception(Message);
            }

            Result = OpResult.OpSuccess;
            Serilog.Log.Information("End unmapping-study data and image");
            OperationContext.WriteSuccessRecord();
            return true;
        }

        /// <summary>
        /// 反批配復原到原本的資料
        /// </summary>
        /// <param name="dcmFile"></param>
        /// <param name="dcmHelper"></param>
        /// <returns></returns>
        private bool UnmappingDatasetToDcmFile(List<DataCorrection.V1.DcmTagData> originalDatasets, DicomFile dcmFile,
            DicomOperatorHelper dcmHelper)
        {
            try
            {
                DicomDataset dataset = dcmFile.Dataset;
                string value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset,
                    DicomTag.SpecificCharacterSet.Group,
                    DicomTag.SpecificCharacterSet.Element, false);
                bool isUtf8 = value.Contains("192");

                originalDatasets.ForEach(data =>
                {
                    DicomTag dTag = new((ushort)data.Group, (ushort)data.Elem);
                    if (dcmHelper.IsSequenceTag(dTag))
                    {
                        // 創建新的 DicomDataset 來存放取出的 Sequence 資料集
                        var newSQDataset = new DicomDataset();

                        // 遍歷 Sequence 中的每一個項目，並將其添加到新的資料集中
                        foreach (var item in data.SeqDcmTagData)
                        {
                            // 遞迴處理嵌套的 sequence
                            WriteDicomValueInDataset(item, newSQDataset, isUtf8, dcmHelper);
                        }

                        // 將新的資料集添加到原始資料集中
                        if (newSQDataset.Any()) dataset.AddOrUpdate(dTag, newSQDataset);
                        // 如果是空的 sequence，則直接刪除
                        else dataset.Remove(dTag);
                    }
                    // 非 sequence，直接寫入值
                    else
                    {
                        dcmHelper.WriteDicomValueInDataset(dataset, dTag, data.Value, isUtf8);
                    }
                });
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 將外部資料更新到DICOM檔案
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        private void WriteDicomValueInDataset(
            DataCorrection.V1.DcmTagData dtagData,
            DicomDataset dataset,
            bool isUtf8,
            DicomOperatorHelper dcmHelper)
        {
            try
            {
                // 創建 DicomTag
                DicomTag dicomTag = new DicomTag((ushort)dtagData.Group, (ushort)dtagData.Elem);
                bool isSequence = dcmHelper.IsSequenceTag(dicomTag);

                if (isSequence)
                {
                    // 創建新的 DicomDataset 來存放取出的 Sequence 資料集
                    DicomDataset newSQDataset = new DicomDataset();

                    // 遍歷 Sequence 中的每一個項目，並將其添加到新的資料集中
                    foreach (var item in dtagData.SeqDcmTagData)
                    {
                        // 遞迴處理嵌套的 sequence
                        WriteDicomValueInDataset(item, newSQDataset, isUtf8, dcmHelper);
                    }

                    // 將新的資料集添加到原始資料集中
                    dataset.AddOrUpdate(dicomTag, newSQDataset);
                }
                // 非 sequence，直接寫入值
                else
                {
                    var originalValue =
                        dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, dicomTag.Group, dicomTag.Element,
                            isUtf8);
                    if (originalValue != dtagData.Value)
                    {
                        dcmHelper.WriteDicomValueInDataset(dataset, dicomTag, dtagData.Value, isUtf8);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
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
            return dcmFile;
        }

        /// <summary>
        /// 更新檢查層Table
        /// </summary>
        /// <returns></returns>
        protected override async Task UpdateDicomTableToDatabase()
        {
            // if (NeedUpdatePatientTable == true)
            //     DcmPatientUidTable.UpdatePatientId(OriginalPatientID, Data.ModifyUser);

            TobeDcmStudyUidTable.UpdateUpdateInstanceUID(insUid: TobeDcmStudyUidTable.StudyInstanceUID.Value,
                    updateUID: OriginalStudyInstanceUID, Data.ModifyUser)
                .SetPatientId(OriginalPatientID);
            await base.UpdateDicomTableToDatabase();
        }

        #endregion

        /// <summary>
        /// 更新Study QC操作狀態
        /// </summary>
        /// <returns></returns>
        protected async Task UpdateStudyMaintainStatusToDatabase(string studyInstanceUID)
        {
            DcmStudyQCStatusTable = new DicomStudyQCStatusTable(Data.ModifyUser);
            DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(studyInstanceUID,
                CommandFieldEvent.StudyMaintainType.Mapped, 0);
            DbCmdService.TableElement = DcmStudyQCStatusTable;
            bool result = await DbCmdService.AddOrUpdate(true);
        }

        /// <summary>
        /// 查詢UID四層表格
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="studyUID"></param>
        /// <returns></returns>
        protected override async Task<bool> QueryUidTable(string userid, string studyUID, bool genNewUid = false)
        {
            bool result = await base.QueryUidTable(userid, studyUID, genNewUid);
            // if (result)
            // {
            //     // 如果舊的PatientId還有存在於Patient層，就不用去修改Patient層
            //     NeedUpdatePatientTable = await CountOfStudy(Data.PatientId) <= 1;
            //     if (NeedUpdatePatientTable)
            //     {
            //         // 如果新的PatientId都沒有在Study層，就去修改Patient層
            //         NeedUpdatePatientTable = await CountOfPatient(NewPatientID) < 1;
            //     }
            // }

            return result;
        }
    }

    #endregion
}