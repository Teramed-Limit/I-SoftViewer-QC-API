using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Table;

namespace ISoftViewerLibrary.Services
{
    #region QcStudyCmdService<T1>

    /// <summary>
    /// Study QC基底物件
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    public abstract class QcStudyCmdService<T1> : IAsyncCommandExecutor
        where T1 : class
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        public QcStudyCmdService(DbQueriesService<CustomizeTable> dbQryService)
        {
            CommandName = "";
            Type = ExecuteType.stInstant;
            Timing = EventTime.etBefore;
            Message = "";
            Result = OpResult.OpSuccess;
            UnmodifiedImageList = new List<string>();
            NewlyGeneratedImageList = new List<string>();
            Messages = new List<string>();
            DbQryService = dbQryService;
            DeviceIdMappingTable = new Dictionary<string, string>();
        }

        #region Fields

        /// <summary>
        /// 命令名稱
        /// </summary>
        public string CommandName { get; protected set; }

        /// <summary>
        /// 執行器類型
        /// </summary>
        public ExecuteType Type { get; protected set; }

        /// <summary>
        /// 執行時間點
        /// </summary>
        public EventTime Timing { get; protected set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; protected set; }

        /// <summary>
        /// 訊息列表
        /// </summary>
        public List<string> Messages;

        /// <summary>
        /// 修改前的檔案列表
        /// </summary>
        public List<string> UnmodifiedImageList;

        /// <summary>
        /// 修改後的影像列表
        /// </summary>
        public List<string> NewlyGeneratedImageList;

        /// <summary>
        /// 資料庫查詢服務
        /// </summary>
        protected DbQueriesService<CustomizeTable> DbQryService;

        /// <summary>
        /// 儲存檔案裝置對應表
        /// </summary>
        protected Dictionary<string, string> DeviceIdMappingTable;

        /// <summary>
        /// 要處理的DICOM三層(不包含PatientID)表格
        /// </summary>
        protected DicomStudyUniqueIdentifiersTable TobeDcmStudyUidTable;

        /// <summary>
        /// DicomStudy QC操作紀錄表格
        /// </summary>
        protected DicomStudyQCStatusTable DcmStudyQCStatusTable;

        /// <summary>
        /// QueryUid時DicomStudy table
        /// </summary>
        protected CustomizeTable QueryUidStudyTable;

        // <summary>
        /// 本地C-Store Node
        /// </summary>
        protected DicomOperationNodes LocalCStoreNode;

        #endregion

        #region Methos

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <returns></returns>
        public abstract Task<bool> Execute();

        /// <summary>
        /// 註冊資料
        /// </summary>
        public abstract Task RegistrationData(object data);

        /// <summary>
        /// 註冊QC操作資料
        /// </summary>
        public abstract void RegistrationOperationContext(QCOperationContext operationContext);

        /// <summary>
        /// 檔案及資料都成功後,刪除原本的影像檔案
        /// </summary>
        protected void AfterSuccessfulThenDeleteOldDcmFiles()
        {
            UnmodifiedImageList.ForEach(filePath =>
            {
                //刪除原來的DICOM檔案
                if (File.Exists(filePath) == true)
                    File.Delete(filePath);
                //刪除原來的JPEG檔案
                string jpgFilePath = Path.ChangeExtension(filePath, ".jpg");
                if (File.Exists(jpgFilePath) == true)
                    File.Delete(jpgFilePath);
            });
        }

        /// <summary>
        /// 失敗,需要將已處理的影像檔案刪除
        /// </summary>
        protected void FailedToDeleteNewImageFiles()
        {
            NewlyGeneratedImageList.ForEach(filePath =>
            {
                //刪除原來的DICOM檔案
                if (File.Exists(filePath) == true)
                    File.Delete(filePath);
                //刪除原來的JPEG檔案
                string jpgFilePath = Path.ChangeExtension(filePath, ".jpg");
                if (File.Exists(jpgFilePath) == true)
                    File.Delete(jpgFilePath);
            });
        }

        /// <summary>
        /// 修改DICOM Tag資料
        /// </summary>
        /// <returns></returns>
        protected virtual bool ModifyDicomTag(DicomFile dcmFile, string newPatientId, string newStudyUID,
            string newSeriesUID, out string newImageUID)
        {
            try
            {
                DicomDataset dcmDataset = dcmFile.Dataset;
                if (dcmDataset == null)
                    throw new Exception("        Cannot get DICOM Dataset ");

                DicomOperatorHelper dicomOperator = new();
                string patientID =
                    dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.PatientID, DicomVR.UI, false);
                string studyUID =
                    dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.StudyInstanceUID, DicomVR.UI, false);
                string seriesUID =
                    dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.SeriesInstanceUID, DicomVR.UI, false);
                string sopUID =
                    dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.SOPInstanceUID, DicomVR.UI, false);
                string sopClassUID =
                    dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.SOPClassUID, DicomVR.UI, false);
                //寫入Patient ID Referenced                
                WriteReferencedUidInDataset(dcmDataset, DicomTag.ReferencedPatientAliasSequence,
                    DicomTag.ReferencedSOPClassUID, sopClassUID,
                    DicomTag.PatientID, patientID);
                //寫入Study Referenced
                WriteReferencedUidInDataset(dcmDataset, DicomTag.ReferencedStudySequence,
                    DicomTag.ReferencedSOPClassUID, sopClassUID,
                    DicomTag.ReferencedSOPInstanceUID, studyUID);
                //寫入Series Referenced
                WriteReferencedUidInDataset(dcmDataset, DicomTag.ReferencedSeriesSequence,
                    DicomTag.ReferencedSOPClassUID, sopClassUID,
                    DicomTag.SeriesInstanceUID, seriesUID);
                //寫入SOP Referenced
                WriteReferencedUidInDataset(dcmDataset, DicomTag.ReferencedImageSequence,
                    DicomTag.ReferencedSOPClassUID, sopClassUID,
                    DicomTag.ReferencedSOPInstanceUID, sopUID);

                //先產生SOP Instance UID資料
                int instanceNumber =
                    Convert.ToInt32(dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.InstanceNumber, DicomVR.IS,
                        false));
                newImageUID = newSeriesUID + "." + Convert.ToString(instanceNumber);
                //寫入Patient ID
                dicomOperator.WriteDicomValueInDataset(dcmDataset, DicomTag.PatientID, newPatientId, false);
                //寫入三層資料
                dicomOperator.WriteDicomValueInDataset(dcmDataset, DicomTag.StudyInstanceUID, newStudyUID, false);
                dicomOperator.WriteDicomValueInDataset(dcmDataset, DicomTag.SeriesInstanceUID, newSeriesUID, false);
                dicomOperator.WriteDicomValueInDataset(dcmDataset, DicomTag.SOPInstanceUID, newImageUID, false);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                newImageUID = "";
                Result = OpResult.OpFailure;
                return false;
            }

            Result = OpResult.OpSuccess;
            return true;
        }

        /// <summary>
        /// 將資料寫入到DicomDataset之中
        /// </summary>
        protected void WriteReferencedUidInDataset(DicomDataset dataset, DicomTag seqTag, DicomTag sopClassUidTag,
            string sopUID, DicomTag sopInstanceUidTag, string insUID)
        {
            DicomOperatorHelper dicomOperator = new();
            DicomSequence sequenceElem;
            DicomDataset item;
            if (dataset.Contains(seqTag) == true)
            {
                sequenceElem = dataset.GetSequence(DicomTag.ReferencedStudySequence);
                item = sequenceElem.Items[0];
                dicomOperator.WriteDicomValueInDataset(item, sopClassUidTag, sopUID, false);
                dicomOperator.WriteDicomValueInDataset(item, sopInstanceUidTag, insUID, false);
            }
            else
            {
                item = new DicomDataset();
                dicomOperator.WriteDicomValueInDataset(item, sopClassUidTag, sopUID, false);
                dicomOperator.WriteDicomValueInDataset(item, sopInstanceUidTag, insUID, false);

                sequenceElem = new DicomSequence(seqTag, item);
                dataset.Add(sequenceElem);
            }
        }

        /// <summary>
        /// 移除Referenced UID Sequence
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="seqTag"></param>
        protected void RemoveReferencedUidInDataset(DicomDataset dataset, DicomTag seqTag)
        {
            if (dataset.Contains(seqTag) == true)
            {
                DicomSequence sequenceElem = dataset.GetSequence(seqTag);
                //把內容清除
                if (sequenceElem != null)
                    sequenceElem.Items.Clear();
                //並移除Sequence Item
                _ = dataset.Remove(seqTag);
            }
        }

        /// <summary>
        /// 取得Referenced Sequence Item
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="seqTag"></param>
        /// <param name="getTag"></param>
        /// <returns></returns>
        protected string GetReferencedUidFromDataset(DicomDataset dataset, DicomTag seqTag, DicomTag getTag)
        {
            DicomOperatorHelper dicomOperator = new();
            string result = string.Empty;
            if (dataset.Contains(seqTag) == true)
            {
                DicomSequence sequenceElem = dataset.GetSequence(seqTag);
                if (sequenceElem != null)
                {
                    foreach (var item in sequenceElem.Items)
                    {
                        result = dicomOperator.GetDicomValueToStringWithGroupAndElem(item, getTag.Group, getTag.Element,
                            false);
                        if (result != string.Empty)
                            break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 查詢裝置資料
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> QueryStorageDevice()
        {
            try
            {
                List<PairDatas> pkeys = new();
                List<PairDatas> nkeys = new()
                {
                    { new PairDatas { Name = "StorageDeviceID", Value = "" } },
                    { new PairDatas { Name = "StoragePath", Value = "" } },
                    { new PairDatas { Name = "StorageDescription", Value = "" } },
                    { new PairDatas { Name = "StorageLevel", Value = "" } },
                    { new PairDatas { Name = "DicomFilePathRule", Value = "" } }
                };
                var dataset = await DbQryService.BuildTable("StorageDevice", pkeys, nkeys)
                    .GetDataAsync();
                dataset.DBDatasets.ForEach(x =>
                {
                    ICommonFieldProperty deviceId = x.Find(field => field.FieldName == "StorageDeviceID");
                    ICommonFieldProperty storagePath = x.Find(field => field.FieldName == "StoragePath");
                    if (deviceId == null || storagePath == null)
                        throw new Exception("     Illegal storage device field");
                    if (deviceId.Value == "" || storagePath.Value == "")
                        throw new Exception("     Illegal storage device value");
                    DeviceIdMappingTable.Add(deviceId.Value, storagePath.Value);
                });
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                return false;
            }

            Result = OpResult.OpSuccess;
            return true;
        }

        /// <summary>
        /// 查詢QC後上傳的DICOM Node
        /// </summary>
        /// <returns></returns>
        protected async void QueryLocalCStoreNode()
        {
            try
            {
                var node = new DicomOperationNodes();
                List<PairDatas> pkeys = new()
                {
                    { new PairDatas { Name = "OperationType", Value = "C-STORE" } },
                    { new PairDatas { Name = "Enable", Value = "1", Type = FieldType.ftInt } },
                    { new PairDatas { Name = "IsLocalStoreService", Value = "1", Type = FieldType.ftInt } },
                };
                List<PairDatas> nkeys = new()
                {
                    { new PairDatas { Name = "Name", Value = "" } },
                    { new PairDatas { Name = "OperationType", Value = "C-STORE" } },
                    { new PairDatas { Name = "AETitle", Value = "" } },
                    { new PairDatas { Name = "RemoteAETitle", Value = "" } },
                    { new PairDatas { Name = "IPAddress", Value = "" } },
                    { new PairDatas { Name = "Port", Value = "", Type = FieldType.ftInt } },
                    { new PairDatas { Name = "Enable", Value = "1", Type = FieldType.ftInt } },
                    { new PairDatas { Name = "IsLocalStoreService", Value = "1", Type = FieldType.ftInt } },
                };
                var dataset = await DbQryService
                    .BuildTable("DicomOperationNodes", pkeys, nkeys)
                    .GetDataAsync();

                if (dataset == null || dataset.DBDatasets.Any() == false)
                    throw new Exception("Local C-Store Node not found !!");

                var dbDataset = dataset.DBDatasets.FirstOrDefault();
                node.Name = dbDataset.Find(field => field.FieldName == "Name").Value;
                node.OperationType = dbDataset.Find(field => field.FieldName == "OperationType").Value;
                node.AETitle = dbDataset.Find(field => field.FieldName == "AETitle").Value;
                node.RemoteAETitle = dbDataset.Find(field => field.FieldName == "RemoteAETitle").Value;
                node.IPAddress = dbDataset.Find(field => field.FieldName == "IPAddress").Value;
                node.Port = int.Parse(dbDataset.Find(field => field.FieldName == "Port").Value);
                Result = OpResult.OpSuccess;
                LocalCStoreNode = node;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                return;
            }
        }

        /// <summary>
        /// 查詢UID表格
        /// </summary>
        /// <param name="studyUID"></param>
        /// <returns></returns>
        protected virtual async Task<bool> QueryUidTable(string userid, string studyUID, bool genNewUid = false)
        {
            try
            {
                var studyTable = await QueryStudyTable(studyUID);
                QueryUidStudyTable = studyTable;
                string st_patientid = TableElementHelper.FindFieldFromDataset(studyTable, "PatientId", 0).Value;
                string st_referenceUid = TableElementHelper
                    .FindFieldFromDataset(studyTable, "ReferencedStudyInstanceUID", 0).Value;

                TobeDcmStudyUidTable = new(userid);
                TobeDcmStudyUidTable
                    .SetInstanceUID(studyUID, studyUID, st_referenceUid)
                    .SetPatientId(st_patientid);

                //取得要被處理的系列表格資料
                var seriesTable = await QuerySeriesTable(studyUID);
                if (seriesTable == null || seriesTable.DBDatasets.Any() == false)
                    throw new Exception("     Query to be series table failed !!");

                for (int idxOfSe = 0; idxOfSe < seriesTable.DBDatasets.Count; idxOfSe++)
                {
                    //建立系列UID表格
                    DicomSeriesUniqueIdentifiersTable tobeSeriesTable = new(userid);
                    string st_uid = studyUID;
                    string ref_st_uid = TableElementHelper
                        .FindFieldFromDataset(seriesTable, "ReferencedStudyInstanceUID", idxOfSe).Value;
                    string se_uid = TableElementHelper.FindFieldFromDataset(seriesTable, "SeriesInstanceUID", idxOfSe)
                        .Value;
                    string newSeUid = genNewUid ? DicomUIDGenerator.GenerateDerivedFromUUID().UID : se_uid;
                    string ref_se_uid = TableElementHelper
                        .FindFieldFromDataset(seriesTable, "ReferencedSeriesInstanceUID", idxOfSe).Value;
                    tobeSeriesTable.SetInstanceUID(se_uid, newSeUid, studyUID, ref_st_uid, ref_se_uid);
                    //查詢系列底下的影像表格
                    var imageTable = await QueryImageTable(se_uid);
                    if (imageTable == null || imageTable.DBDatasets.Any() == false)
                        throw new Exception("     Query to be image table failed !!");

                    for (int idxOfImg = 0; idxOfImg < imageTable.DBDatasets.Count; idxOfImg++)
                    {
                        DicomImageUniqueIdentifiersTable tobeImageTable = new(userid);
                        string sopUid = TableElementHelper.FindFieldFromDataset(imageTable, "SOPInstanceUID", idxOfImg)
                            .Value;
                        string newSopUid = genNewUid ? DicomUIDGenerator.GenerateDerivedFromUUID().UID : sopUid;
                        string stgDevice = TableElementHelper
                            .FindFieldFromDataset(imageTable, "StorageDeviceID", idxOfImg).Value;
                        string filePath = TableElementHelper.FindFieldFromDataset(imageTable, "FilePath", idxOfImg)
                            .Value;
                        string refSopUid = TableElementHelper
                            .FindFieldFromDataset(imageTable, "ReferencedSOPInstanceUID", idxOfImg).Value;
                        string refSerUid = TableElementHelper
                            .FindFieldFromDataset(imageTable, "ReferencedSeriesInstanceUID", idxOfImg).Value;
                        string mapTag = TableElementHelper.FindFieldFromDataset(imageTable, "UnmappedDcmTags", idxOfImg)
                            .Value;

                        tobeImageTable.SetInstanceUID(sopUid, newSopUid, newSeUid, stgDevice, filePath, refSerUid,
                            refSopUid,
                            mapTag);
                        tobeSeriesTable.DetailElements.Add(tobeImageTable);
                    }

                    TobeDcmStudyUidTable.DetailElements.Add(tobeSeriesTable);
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                return false;
            }

            Result = OpResult.OpSuccess;
            return true;
        }

        /// <summary>
        /// 查詢系列表格
        /// </summary>
        /// <param name="studyUID"></param>
        /// <returns></returns>
        protected async Task<CustomizeTable> QueryStudyTable(string studyUID)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = studyUID } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = "" } },
                { new PairDatas { Name = "PatientId", Value = "" } },
                { new PairDatas { Name = "ReferencedStudyInstanceUID", Value = "" } },
                { new PairDatas { Name = "AccessionNumber", Value = "" } }
            };
            var dataset = await DbQryService
                .BuildTable("DicomStudy", pkeys, nkeys)
                .GetDataAsync();

            return dataset;
        }

        protected async Task MakeSureStudyExist(string studyUID)
        {
            bool conditionMet = false;

            // Continue looping until the condition is met
            while (!conditionMet)
            {
                // Perform the async operation and get the result
                var dataset = await QueryStudyTable(studyUID);
                conditionMet = dataset.DBDatasets.Count > 0;

                // Optional: Add a delay to avoid a tight loop
                if (conditionMet == false) await Task.Delay(1000);
            }
        }

        protected async Task<bool> DoseStudyExist(string studyUID)
        {
            var dataset = await QueryStudyTable(studyUID);
            return dataset.DBDatasets.Count > 0;
        }

        /// <summary>
        /// 查詢系列表格
        /// </summary>
        /// <param name="studyUID"></param>
        /// <returns></returns>
        protected async Task<CustomizeTable> QuerySeriesTable(string studyUID)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = studyUID } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "SeriesInstanceUID", Value = "" } },
                { new PairDatas { Name = "ReferencedStudyInstanceUID", Value = "" } },
                { new PairDatas { Name = "ReferencedSeriesInstanceUID", Value = "" } }
            };
            var dataset = await DbQryService.BuildTable("DicomSeries", pkeys, nkeys)
                .GetDataAsync();
            return dataset;
        }

        /// <summary>
        /// 查詢影像表格
        /// </summary>
        /// <param name="seriesUID"></param>
        /// <returns></returns>
        protected virtual async Task<CustomizeTable> QueryImageTable(string seriesUID)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "SeriesInstanceUID", Value = seriesUID } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "SOPInstanceUID", Value = "" } },
                { new PairDatas { Name = "StorageDeviceID", Value = "" } },
                { new PairDatas { Name = "FilePath", Value = "" } },
                { new PairDatas { Name = "ReferencedSOPInstanceUID", Value = "" } },
                { new PairDatas { Name = "ReferencedSeriesInstanceUID", Value = "" } },
                { new PairDatas { Name = "UnmappedDcmTags", Value = "" } }
            };
            var dataset = await DbQryService.BuildTable("DicomImage", pkeys, nkeys)
                .GetDataAsync();
            return dataset;
        }

        /// <summary>
        /// 垃坄回收機制
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region QcStudyCmdWithDcmNetService<T1>

    /// <summary>
    /// Study QC基底物件(包含DICOM Net服務)
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    public abstract class QcStudyCmdWithDcmNetService<T1> : QcStudyCmdService<T1>
        where T1 : class
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dcmUnitOfWork"></param>
        /// <param name="dcmCqusDatasts"></param>
        public QcStudyCmdWithDcmNetService(
            DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService,
            IDcmUnitOfWork dcmUnitOfWork,
            IDcmCqusDatasets dcmCqusDatasts,
            EnvironmentConfiguration publicConfig,
            IEnumerable<SvrConfigurationsV2> svrConfiguration)
            : base(dbQryService)
        {
            DbCmdService = dbCmdService;
            DcmUnitOfWork = dcmUnitOfWork;
            DcmCqusDatasets = dcmCqusDatasts;
            EnvirConfig = publicConfig;
            SvrConfiguration = svrConfiguration.ToList();

            NewPatientID = "";
            OriginalPatientID = "";
            NewStudyInstanceUID = "";
            OriginalStudyInstanceUID = "";
            NeedUpdatePatientTable = false;
        }


        #region Fields

        /// <summary>
        /// 資料庫更新服務
        /// </summary>
        protected DbCommandService<CustomizeTable> DbCmdService;

        /// <summary>
        /// DICOM服務單一作業流程
        /// </summary>
        protected IDcmUnitOfWork DcmUnitOfWork;

        /// <summary>
        /// Dataset Repository輔助物件
        /// </summary>
        protected IDcmCqusDatasets DcmCqusDatasets;

        /// <summary>
        /// 全域組態
        /// </summary>
        protected EnvironmentConfiguration EnvirConfig;

        /// <summary>
        /// 新的病歷號碼
        /// </summary>
        protected string NewPatientID;

        /// <summary>
        /// 目前檢查的病歷號碼
        /// </summary>
        protected string OriginalPatientID;

        /// <summary>
        /// Mapping指定的StudyInstanceUID
        /// </summary>
        protected string NewStudyInstanceUID;

        /// <summary>
        /// 目前檢查的唯一碼
        /// </summary>
        protected string OriginalStudyInstanceUID;

        /// <summary>
        /// DicomPatient唯一碼表格
        /// </summary>
        protected DicomPatientUniqueIdentifiersTable DcmPatientUidTable;

        /// <summary>
        /// 是否需要更新Patient Table
        /// </summary>
        protected bool NeedUpdatePatientTable;

        /// <summary>
        /// SystemConfig Table
        /// </summary>
        protected List<SvrConfigurationsV2> SvrConfiguration { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// 印出Log
        /// </summary>
        protected void PrintLog()
        {
            foreach (var message in Messages)
            {
                Serilog.Log.Information(message);
            }
        }

        /// <summary>
        /// 查詢病歷號表格
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        protected async virtual Task<CustomizeTable> QueryPatientIdTable(string patientId)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "PatientId", Value = patientId } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "PatientId", Value = "" } }
            };
            var dataset = await DbQryService.BuildTable("DicomPatient", pkeys, nkeys)
                .GetDataAsync();
            return dataset;
        }

        /// <summary>
        /// 確認該病患的檢查數量
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        protected async Task<int> CountOfStudy(string patientId)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "PatientId", Value = patientId } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = "" } }
            };
            var dataset = await DbQryService.BuildTable("DicomStudy", pkeys, nkeys)
                .GetDataAsync();
            return dataset.DBDatasets.Count;
        }

        /// <summary>
        /// 確認該病患的檢查數量
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        protected async Task<int> CountOfPatient(string patientId)
        {
            List<PairDatas> pkeys = new()
            {
                { new PairDatas { Name = "PatientId", Value = patientId } }
            };
            List<PairDatas> nkeys = new()
            {
                { new PairDatas { Name = "PatientId", Value = "" } }
            };
            var dataset = await DbQryService.BuildTable("DicomPatient", pkeys, nkeys)
                .GetDataAsync();
            return dataset.DBDatasets.Count;
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
            if (result == true)
            {
                string patientId = TobeDcmStudyUidTable.PatientID.Value;
                // 先確認要Mapping檢查的Patient底下是否還有其它Study,如果有,不處理Patient,如果沒有,要更新DicomPatient資料表
                NeedUpdatePatientTable = await CountOfStudy(patientId) <= 1;
                if (NeedUpdatePatientTable == true)
                {
                    if (DcmPatientUidTable == null)
                        DcmPatientUidTable = new DicomPatientUniqueIdentifiersTable(userid);
                    DcmPatientUidTable.SetPatientId(patientId, patientId);
                    DcmPatientUidTable.DetailElements.Clear();
                    DcmPatientUidTable.DetailElements.Add(TobeDcmStudyUidTable);
                }
            }

            return result;
        }

        /// <summary>
        /// 依據Table內容去取得DicomFile物件
        /// </summary>
        /// <param name="imgUidTable"></param>
        /// <param name="dcmHelper"></param>
        /// <param name="dcmFilePath"></param>
        /// <returns></returns>
        protected abstract DicomFile GetDicomFile(DicomImageUniqueIdentifiersTable imgUidTable,
            DicomOperatorHelper dcmHelper,
            ref string dcmFilePath);

        /// <summary>
        /// 更新檢查層Table
        /// </summary>
        /// <returns></returns>
        protected virtual async Task UpdateDicomTableToDatabase()
        {
            string tableName;
            if (NeedUpdatePatientTable == true)
            {
                DbCmdService.TableElement = DcmPatientUidTable;
                tableName = "DicomPatient";
            }
            else
            {
                DbCmdService.TableElement = TobeDcmStudyUidTable;
                tableName = "DicomStudy";
            }

            bool result = await DbCmdService.AddOrUpdate(true);
            foreach (var msg in TobeDcmStudyUidTable.GetMessages())
            {
                Messages.Add(msg);
            }

            if (result == false)
                throw new Exception($"    *** This is a problem with the execute the {tableName} Table *** ");
        }

        /// <summary>
        /// 儲存DICOM檔案
        /// </summary>
        /// <param name="dcmFiles"></param>
        /// <param name="dcmHelper"></param>
        /// <param name="addInCServiceHelperRep"></param>
        /// <returns></returns>
        protected bool SaveDcmToFile(Dictionary<string, DicomFile> dcmFiles, bool addInCServiceHelperRep = false)
        {
            try
            {
                if (addInCServiceHelperRep == true)
                    DcmCqusDatasets.DicomDatasets.Clear();

                foreach (var data in dcmFiles)
                {
                    DicomFile dcmFile = data.Value;
                    //放入Helper DcmDataset之中
                    if (addInCServiceHelperRep == true)
                        DcmCqusDatasets.DicomDatasets.Add(dcmFile.Dataset);
                }
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
        /// 傳送DICOM檔案到遠端DICOM Service Provider
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> SendDcmToScp(Dictionary<string, DicomFile> dcmFiles)
        {
            bool result = false;
            try
            {
                DcmCqusDatasets.DicomDatasets.Clear();
                foreach (var data in dcmFiles)
                {
                    //避免檔案獨佔,所以要另外建立DicomDataset,在把之前釋放
                    DicomFile dcmFile = data.Value;
                    //Modify 20220524 Oscar 改用clone的方式避開資料檢核(AutoValidate屬性)
                    DicomDataset dataset = dcmFile.Dataset.Clone();

                    Console.WriteLine(dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));
                    Console.WriteLine(dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                    Console.WriteLine(dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID));
                    DcmCqusDatasets.DicomDatasets.Add(dataset);
                    dcmFile = null;
                }

                dcmFiles.Clear();

                DcmUnitOfWork.Begin(
                    LocalCStoreNode.IPAddress,
                    LocalCStoreNode.Port,
                    LocalCStoreNode.AETitle,
                    LocalCStoreNode.RemoteAETitle,
                    Types.DcmServiceUserType.dsutStore);
                DcmUnitOfWork.RegisterRepository(DcmCqusDatasets);

                if (await DcmUnitOfWork.Commit() == false)
                    throw new Exception(DcmUnitOfWork.Message);
                result = true;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return await Task.FromResult(result);
        }

        #endregion
    }

    #endregion
}