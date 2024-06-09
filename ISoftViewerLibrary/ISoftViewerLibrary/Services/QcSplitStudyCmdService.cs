using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Services
{
    #region QcSplitStudyCmdService
    /// <summary>
    /// 拆解檢查命令服務
    /// </summary>
    public class QcSplitStudyCmdService : QcStudyCmdService<DataCorrection.V1.SplitStudyParameter>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        /// <param name="environmentConfiguration"></param>
        /// <param name="svrConfiguration"></param>
        public QcSplitStudyCmdService(DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService,
            EnvironmentConfiguration environmentConfiguration, IEnumerable<SvrConfigurationsV2> svrConfiguration) 
            : base(dbQryService)
        {
            DbCmdService = dbCmdService;
            MergeSplitMappingTagTable = environmentConfiguration.MergeSplitMappingTagTable;
        }

        #region Fields
        /// <summary>
        /// 資料庫更新服務
        /// </summary>
        protected DbCommandService<CustomizeTable> DbCmdService;
        /// <summary>
        /// 註冊資料
        /// </summary>
        protected DataCorrection.V1.SplitStudyParameter Data;
        /// <summary>
        /// QC操作紀錄
        /// </summary>
        private QCOperationContext OperationContext { get; set; }
        /// <summary>
        /// Split時mapping table
        /// </summary>
        private readonly List<FieldToDcmTagMap> MergeSplitMappingTagTable;
        #endregion

        #region Methods

        /// <summary>
        /// 註冊資料
        /// </summary>
        /// <param name="data"></param>
        public override async Task RegistrationData(object data)
        {
            Data = (DataCorrection.V1.SplitStudyParameter)data;
        }
        /// <summary>
        /// 註冊Study操作資料
        /// </summary>
        /// <param name="operationContext"></param>
        public override void RegistrationOperationContext(QCOperationContext operationContext)
        {
            OperationContext = operationContext;
            OperationContext.SetLogger(new SplitStudyLogger());
        }
        /// <summary>
        /// 執行命令
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Execute()
        {
            try
            {
                Messages.Add("    *** Start split study data and image merge ***");
                Messages.Add("     ** Split Study Instance UID From " + Data.StudyInstanceUID);

                //取得要拆解的檢查,系列和影像資料
                if (await QueryUidTable(Data.ModifyUser, Data.StudyInstanceUID) == false || 
                    await QueryStorageDevice() == false)
                    return false;
                
                //建立要拆解回復的檢查層表格
                DicomStudyUniqueIdentifiersTable splitToStudyUidTable = null;                
                foreach (DicomSeriesUniqueIdentifiersTable _seTable in TobeDcmStudyUidTable.DetailElements)
                {
                    //沒有記載Ref Series Instance UID & Ref Study Instance UID,則代表沒有執行被合併的動作
                    if (_seTable.ReferencedSeriesInstanceUID.Value == "" || _seTable.ReferencedStudyInstanceUID.Value == "")
                        continue;

                    //建立拆解回復的檢查層表格
                    if (splitToStudyUidTable == null)
                        splitToStudyUidTable = CreateSplitRestoredStudyTable(_seTable);
                    //建立拆解回復的系列層表格
                    DicomSeriesUniqueIdentifiersTable splitToSeriesUidTable = CreateSplitRestoredSeriesTable(_seTable);
                    splitToStudyUidTable.DetailElements.Add(splitToSeriesUidTable);

                    //處理影像(有ReferenceSOPInstanceUID欄位資料,才是從別的檢查合併過來的)
                    foreach (DicomImageUniqueIdentifiersTable imageTable in _seTable.DetailElements)
                    {
                        if (imageTable is not DicomImageUniqueIdentifiersTable _imgTable)
                            throw new Exception("        Illegal image table");

                        //判斷是否需要拆解,沒有ReferenceSOPInstanceUID資料則不需要拆解
                        if (_imgTable.ReferencedSOPInstanceUID.Value == "")
                            continue;
                        if (_imgTable.FilePath.Value == "")
                            throw new Exception("        Illegal FilePath field");
                        if (DeviceIdMappingTable.ContainsKey(_imgTable.StorageDeviceID.Value) == false)
                            throw new Exception("        Illegal StorageDeviceID field");

                        //現有檔案路徑
                        string storagePath = DeviceIdMappingTable[_imgTable.StorageDeviceID.Value].Trim();
                        string oldDcmFilePath = storagePath + _imgTable.FilePath.Value.Trim();

                        //尚未支援壓縮檔案格式處理
                        DicomFile dcmFile = DicomFile.Open(oldDcmFilePath);
                        if (dcmFile == null)
                            throw new Exception("        Can not open file : " + oldDcmFilePath);
                        //更換UID資料
                        if (ReplaceDicomUniqueIdentityUID(dcmFile.Dataset, _seTable.ReferencedStudyInstanceUID.Value, _seTable.ReferencedSeriesInstanceUID.Value,
                            _imgTable.ReferencedSOPInstanceUID.Value) == false)
                            throw new Exception("ReplaceDicomUniqueIdentityUID function with a problem ");

                        string newDcmFileName = _imgTable.ReferencedSOPInstanceUID.Value + ".dcm";
                        string newDcmFilePath = Path.Combine(Path.GetDirectoryName(oldDcmFilePath), newDcmFileName);
                        //儲存DICOM檔案
                        dcmFile.Save(newDcmFilePath);
                        if (File.Exists(newDcmFilePath) == false)
                            throw new Exception("        File save failed : " + newDcmFilePath);
                        //儲存JPEG檔案
                        string oldJpegFilePath = Path.ChangeExtension(oldDcmFilePath, ".jpg");
                        string newJpegFilePath = Path.ChangeExtension(newDcmFilePath, ".jpg");
                        File.Copy(oldJpegFilePath, newJpegFilePath, true);

                        string dbFilePath = newDcmFilePath.Replace(storagePath, " ").Trim();
                        //建立拆解回復的影像表格
                        DicomImageUniqueIdentifiersTable splitToImageUidTable = CreateSplitRestoredImageTable(_imgTable);
                        splitToImageUidTable.UpdateFilePath(dbFilePath);                        

                        splitToSeriesUidTable.DetailElements.Add(splitToImageUidTable);

                        //記錄更新前和更新後的檔案路徑
                        UnmodifiedImageList.Add(oldDcmFilePath);
                        NewlyGeneratedImageList.Add(newDcmFilePath);
                    }
                    //更新Study, Series, Image資料表格,因為現在可以合併多個檢查和多個系列,所以每一個系列都更新一次             
                    if (splitToStudyUidTable != null)
                    {                        
                        DbCmdService.TableElement = splitToStudyUidTable;
                        //若要執行二筆以上的AddOrUpdate,則需要使用新的Transaction
                        if (await DbCmdService.AddOrUpdate(true) == false)
                            throw new Exception("    *** This is a problem with the execute the DicomStudy Table *** ");
                        splitToStudyUidTable = null;                        
                    }
                };
                //更新狀態
                await UpdateStudyMaintainStatusToDatabase();
                //QC operation log
                OperationContext.SetParams(Data.ModifyUser, Data.StudyInstanceUID);
            }
            catch (Exception ex)
            {
                //出現錯誤,則將已處理的檔案刪除
                FailedToDeleteNewImageFiles();
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                // OperationContext.WriteFailedRecord(ex.Message, ex.ToString());
                throw new Exception(Message);
            }
            //刪除舊有的Dicom檔案
            if (Data.AfterSplitStudyToDeleteOldFiles == true)            
                AfterSuccessfulThenDeleteOldDcmFiles();           

            Result = OpResult.OpSuccess;
            Messages.Add("    *** Successful split study data images");
            OperationContext.WriteSuccessRecord();
            return true;
        }
        /// <summary>
        /// 建立拆解回復檢查層表格
        /// </summary>
        /// <param name="originalSeriesTable"></param>
        /// <returns></returns>
        protected DicomStudyUniqueIdentifiersTable CreateSplitRestoredStudyTable(DicomSeriesUniqueIdentifiersTable originalSeriesTable)
        {
            DicomStudyUniqueIdentifiersTable restoreStudyUidTable = new(Data.ModifyUser);
            //Where條件 & Update內容
            restoreStudyUidTable.SetInstanceUID(studyUID : originalSeriesTable.ReferencedStudyInstanceUID.Value,
                    updateUID : originalSeriesTable.ReferencedStudyInstanceUID.Value, 
                    refUID : "")
                .SetModifyUser(Data.ModifyUser, DateTime.Now.ToString("yyyyMMddHHmmss"));            

            return restoreStudyUidTable;
        }
        /// <summary>
        /// 建立拆解回復系列層表格
        /// </summary>
        /// <param name="originalSeriesTable"></param>
        /// <returns></returns>
        protected DicomSeriesUniqueIdentifiersTable CreateSplitRestoredSeriesTable(DicomSeriesUniqueIdentifiersTable originalSeriesTable)
        {
            DicomSeriesUniqueIdentifiersTable restoreToSeriesUidTable = new(Data.ModifyUser);
            //Where條件 & Update改回原本的STUDY & SERIES的UID
            restoreToSeriesUidTable.SetInstanceUID(seriesUID : originalSeriesTable.SeriesInstanceUID.Value,
                updateUID : originalSeriesTable.ReferencedSeriesInstanceUID.Value,
                studyUID : originalSeriesTable.ReferencedStudyInstanceUID.Value, 
                refStudyUID : "", refSeriesUID : "")
                .SetModifyUser(Data.ModifyUser, DateTime.Now.ToString("yyyyMMddHHmmss"));                
            //主鍵更換
            restoreToSeriesUidTable.UpdateKeyValueSwap();

            return restoreToSeriesUidTable;
        }
        /// <summary>
        /// 建立拆解回復影像層表格
        /// </summary>
        /// <param name="originalSeriesTable"></param>
        /// <returns></returns>
        protected DicomImageUniqueIdentifiersTable CreateSplitRestoredImageTable(DicomImageUniqueIdentifiersTable originalImageTable)
        {
            DicomImageUniqueIdentifiersTable restoreToImageUidTable = new(Data.ModifyUser);
            //Where條件 & 更新回原本的SERIES & SOP的UID
            restoreToImageUidTable.SetInstanceUID(sopUID: originalImageTable.SOPInstanceUID.Value,
                updateUID: originalImageTable.ReferencedSOPInstanceUID.Value,
                seriesUID: originalImageTable.ReferencedSeriesInstanceUID.Value,
                storageDev: originalImageTable.StorageDeviceID.Value,
                filePath: originalImageTable.FilePath.Value,
                refSeriesUID: "", refSopUID: "", originalImageTable.UnmappedDcmTag.Value)
                .SetModifyUser(Data.ModifyUser, DateTime.Now.ToString("yyyyMMddHHmmss"));
            //主鍵更換
            restoreToImageUidTable.UpdateKeyValueSwap();

            return restoreToImageUidTable;
        }
        /// <summary>
        /// 置換Study, Series, SOP InstanceUID
        /// </summary>
        /// <param name="studyUID"></param>
        /// <param name="seriesUID"></param>
        /// <param name="sopUID"></param>
        /// <returns></returns>
        protected bool ReplaceDicomUniqueIdentityUID(DicomDataset dataset, string studyUID, string seriesUID, string sopUID)
        {
            try
            {
                //原生的PatientID由ReferencedPatientAliasSequence取得
                string patientId = GetReferencedUidFromDataset(dataset, DicomTag.ReferencedPatientAliasSequence, DicomTag.PatientID);

                //移除ReferencedPatientAliasSequence
                RemoveReferencedUidInDataset(dataset, DicomTag.ReferencedPatientAliasSequence);
                //移除Study Referenced
                RemoveReferencedUidInDataset(dataset, DicomTag.ReferencedStudySequence);
                //移除Series Referenced
                RemoveReferencedUidInDataset(dataset, DicomTag.ReferencedSeriesSequence);
                //移除SOP Referenced
                RemoveReferencedUidInDataset(dataset, DicomTag.ReferencedImageSequence);
                
                //接著將三層UID寫入到Dataset之中
                DicomOperatorHelper dicomOperator = new();                
                dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.StudyInstanceUID, studyUID, false);
                dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.SeriesInstanceUID, seriesUID, false);
                dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.SOPInstanceUID, sopUID, false);
                //最後需要在處理PatientID                
                dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.PatientID, patientId, false);

                //更新Dicom基本資訊(設定來自Appsetting)
                var staticField = "[Static]";

                List<PairDatas> pkeys = new()
                {
                    { new PairDatas { Name = "StudyInstanceUID", Value = studyUID } },
                    { new PairDatas { Name = "PatientId", Value = patientId } }
                };

                List<PairDatas> nkeys = MergeSplitMappingTagTable.Where(x => x.Field != staticField).Select(mapper => new PairDatas { Name = mapper.Field, Value = string.Empty }).ToList();

                var table = DbQryService.BuildTable("DcmFindStudyLevelView", pkeys, nkeys).GetData();
                string value = dicomOperator.GetDicomValueToStringWithGroupAndElem(dataset, DicomTag.SpecificCharacterSet.Group,
                    DicomTag.SpecificCharacterSet.Element, false);
                bool isUtf8 = value.Contains("192");

                foreach (var fieldToDcmTagMap in MergeSplitMappingTagTable)
                {
                    var updateValue = "";
                    if (fieldToDcmTagMap.Field == staticField)
                    {
                        updateValue = fieldToDcmTagMap.Default;
                    }
                    else
                    {
                        var dbField = table.DBDatasets.First().First(x => x.FieldName == fieldToDcmTagMap.Field);
                        updateValue = dbField.Value;
                    }
                    dicomOperator.ConvertTagStringToUIntGE(fieldToDcmTagMap.Tag, out ushort t_group, out ushort t_element);
                    dicomOperator.WriteDicomValueInDataset(dataset, new DicomTag(t_group, t_element), updateValue, isUtf8);
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
        /// 更新Study QC操作狀態
        /// </summary>
        /// <returns></returns>
        protected async Task UpdateStudyMaintainStatusToDatabase()
        {
            DcmStudyQCStatusTable = new DicomStudyQCStatusTable(Data.ModifyUser);
            DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(Data.StudyInstanceUID, CommandFieldEvent.StudyMaintainType.Merged, 0);
            DbCmdService.TableElement = DcmStudyQCStatusTable;
            bool result = await DbCmdService.AddOrUpdate(true);
        }
        #endregion
    }
    #endregion
}
