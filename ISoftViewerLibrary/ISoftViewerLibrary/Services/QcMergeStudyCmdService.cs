using Dicom;
using Dicom.Imaging;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Events;

namespace ISoftViewerLibrary.Services
{
    #region QcMergeStudyCmdService
    /// <summary>
    /// 執行合併檢查命令
    /// </summary>
    public class QcMergeStudyCmdService : QcStudyCmdService<DataCorrection.V1.MergeStudyParameter>
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        /// <param name="environmentConfiguration"></param>
        /// <param name="dcmUnitOfWork"></param>
        /// <param name="dcmCqusDatasts"></param>
        public QcMergeStudyCmdService(
            DbQueriesService<CustomizeTable> dbQryService, DbCommandService<CustomizeTable> dbCmdService,
            EnvironmentConfiguration environmentConfiguration) 
            : base(dbQryService)
        {
            DbCmdService = dbCmdService;
            MergeSplitMappingTagTable = environmentConfiguration.MergeSplitMappingTagTable;
        }

        #region Fields
        /// <summary>
        /// 檢查層的表格
        /// </summary>
        protected CustomizeTable OriginalStudyTable;
        /// <summary>
        /// 系列層的表格
        /// </summary>
        protected CustomizeTable OriginalSeriesTable;
        /// <summary>
        /// 資料庫更新服務
        /// </summary>
        protected DbCommandService<CustomizeTable> DbCmdService;
        /// <summary>
        /// 註冊資料
        /// </summary>
        protected DataCorrection.V1.MergeStudyParameter Data;
        /// <summary>
        /// QC操作紀錄
        /// </summary>
        private QCOperationContext OperationContext { get; set; }
        /// <summary>
        /// Merge時mapping table
        /// </summary>
        private readonly List<FieldToDcmTagMap> MergeSplitMappingTagTable;
        #endregion

        #region Methods
        /// <summary>
        /// 註冊資料
        /// </summary>
        /// <param name="data"></param>
        public override void RegistrationData(object data) 
        {
            Data = (DataCorrection.V1.MergeStudyParameter)data;
        }

        /// <summary>
        /// 註冊Study操作資料
        /// </summary>
        /// <param name="operationContext"></param>
        public override void RegistrationOperationContext(QCOperationContext operationContext)
        {
            OperationContext = operationContext;
            OperationContext.SetLogger(new MergeStudyLogger());
        }

        /// <summary>
        /// 執行命令
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Execute()
        {
            try
            {
                Messages.Add("    *** Start cross-study data and image merge ***");
                Messages.Add("     ** Merge Study Instance UID From " + Data.FromStudyUID + " to " + Data.ToStudyUID);

                //取得被合併的檢查,系列和影像資料及裝置資料
                if (await QueryUidTable(Data.ModifyUser, Data.FromStudyUID) == false ||
                    await QueryStorageDevice() == false)
                    throw new Exception("Failed to query to merge from table or storage device data!!");
                
                //取得要合併其它檢查的StudyTable          
                if (CreateOriginalStudyTable(Data.ModifyUser, "DicomStudy", Data.ToStudyUID) == Task.FromResult(false))
                    throw new Exception("Failed to query to merge study table !!");

                string originalPatientId = TableElementHelper.FindNormalKeyField(OriginalStudyTable, "PatientID").Value.Trim();

                //取得要合併其它檢查的SeriesTable          
                if (CreateOriginalSeriesTable(Data.ModifyUser, "DicomSeries", Data.ToStudyUID) == Task.FromResult(false))
                    throw new Exception("Query to merge series table failed !!");    
                                
                //用隨機產生數字,之前用日期會有重覆問題
                Random rdm = new();
                int rdmNumber = rdm.Next(1, 999999);
                string rdmValue = Convert.ToString(rdmNumber);
                ICommonFieldProperty fieldProperty = TableElementHelper.FindField(OriginalStudyTable, "StudyInstanceUID");
                string newSeriesUidRoot;
                //要判斷要合併其它檢查的StudyInstanceUID長度,不然有可能會超出UI的長度限度
                if (fieldProperty.Value.Length >= 50)
                    newSeriesUidRoot = "1.3.6.1.4.1.54514." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".1.2." + rdmValue;
                else
                    newSeriesUidRoot = fieldProperty.Value + ".1.2." + rdmValue;
                int idxOfUid = 1;
                //fo-dicom使用Win Form的ImageManager
                ImageManager.SetImplementation(WinFormsImageManager.Instance);

                //處理系列
                TobeDcmStudyUidTable.DetailElements.ForEach(seriesTable =>
                {
                    if (seriesTable is not DicomSeriesUniqueIdentifiersTable _seTable)
                        throw new Exception("        Illegal series table");
                    //由於儀器送上來的InstanceUID有可能就已經快64碼,在重新封裝,有可能超過UI的限制長度                    
                    string newSeriesUid = newSeriesUidRoot + "." + Convert.ToString(idxOfUid++);
                    //處理影像
                    _seTable.DetailElements.ForEach(imageTable =>
                    {
                        //先組合完整的檔案路徑
                        if (imageTable is not DicomImageUniqueIdentifiersTable _imgTable)
                            throw new Exception("        Illegal image table");
                        if (_imgTable.FilePath.Value == "")
                            throw new Exception("        Illegal FilePath field");
                        if (DeviceIdMappingTable.ContainsKey(_imgTable.StorageDeviceID.Value) == false)
                            throw new Exception("        Illegal StorageDeviceID field");

                        //現有檔案路徑
                        string storagePath = DeviceIdMappingTable[_imgTable.StorageDeviceID.Value].Trim();
                        string filePath = storagePath + _imgTable.FilePath.Value.Trim();
                        //尚未支援壓縮檔案格式處理
                        DicomFile dcmFile = DicomFile.Open(filePath);
                        if (dcmFile == null)
                            throw new Exception("        Can not open file : " + filePath);
                        //修改DICOM Tag
                        if (ModifyDicomTag(dcmFile, originalPatientId, Data.ToStudyUID, newSeriesUid, out string newImgUID) == false)
                            throw new Exception("        DICOM file modification failed : " + filePath);

                        //產生新檔案路徑
                        string newFilePath = Path.Combine(Path.GetDirectoryName(filePath), newImgUID + ".dcm");
                        dcmFile.Save(newFilePath);
                        if (File.Exists(newFilePath) == false)
                            throw new Exception("        File save failed : " + newFilePath);
                        string dbFilePath = newFilePath.Replace(storagePath, " ").Trim();

                        //更新影像表格裡的資料
                        _imgTable.UpdateKeyValueSwap();
                        //MOD BY JB 20210615 如果Image Table的Reference UID欄位已經有值,則保留最原始的資料
                        _imgTable.UpdateReferenceUID(_imgTable.SOPInstanceUID.Value, _seTable.SeriesInstanceUID.Value)                        
                            .UpdateInstanceUIDAndPath(newSeriesUid, newImgUID, dbFilePath, Data.ModifyUser);
                        //產生JPEG預覽圖
                        string dcmFilePath = Path.GetFullPath(newFilePath);
                        string jpgFilePath = Path.ChangeExtension(dcmFilePath, ".jpg");
                        //若檔案存在,則刪除
                        if (File.Exists(jpgFilePath) == true)
                            File.Delete(jpgFilePath);
                        
                        var image = new DicomImage(newFilePath);
#pragma warning disable CA1416 // 驗證平台相容性
                        image.RenderImage().AsClonedBitmap().Save(jpgFilePath);
#pragma warning restore CA1416 // 驗證平台相容性
                              //using (IImage iimage = image.RenderImage())
                              //{
                              //    using (Bitmap bmp = iimage.AsClonedBitmap())
                              //    {
                              //        bmp.Save(jpgFilePath);
                              //    }
                              //}
                              //記錄更新前和更新後的檔案路徑
                        UnmodifiedImageList.Add(filePath);
                        //NewlyGeneratedImageList.Add(newFilePath);
                    });
                    //更新系列資料表資料
                    _seTable.UpdateKeyValueSwap();
                    //MOD BY JB 20210615 如果Series Table的Reference UID欄位已經有值,則保留最原始的資料
                    _seTable.UpdateReferenceInstanceUID(_seTable.SeriesInstanceUID.Value, _seTable.StudyInstanceUID.Value)                    
                        .UpdateInstanceUIDAndData(newSeriesUid, Data.ToStudyUID, Data.ModifyUser);                    

                    Messages.Add("TobeSeriesTable.StudyInstanceUID.Value : " + _seTable.StudyInstanceUID.Value);
                    Messages.Add("TobeSeriesTable.ModifiedUser.Value : " + _seTable.ModifiedUser.Value);
                    Messages.Add("TobeSeriesTable.ReferencedSeriesInstanceUID.Value : " + _seTable.ReferencedSeriesInstanceUID.Value);
                    Messages.Add("TobeSeriesTable.ReferencedStudyInstanceUID.Value : " + _seTable.ReferencedStudyInstanceUID.Value);
                });

                TobeDcmStudyUidTable.UpdateReferenceInstanceUID(TobeDcmStudyUidTable.StudyInstanceUID.Value.Trim(), "Merged", Data.ModifyUser);
                
                Messages.Add("TobeDcmStudyUidTable.UpdateStudyInstanceUID.Value : " + TobeDcmStudyUidTable.StudyInstanceUID.Value);
                Messages.Add("TobeDcmStudyUidTable.ReferencedStudyInstanceUID.Value : " + TobeDcmStudyUidTable.ReferencedStudyInstanceUID.Value);
                Messages.Add("TobeDcmStudyUidTable.ModifiedUser.Value : " + TobeDcmStudyUidTable.ModifiedUser.Value);
                                
                DbCmdService.TableElement = TobeDcmStudyUidTable;
                if (await DbCmdService.AddOrUpdate(true) == false)
                {
                    foreach (var msg in TobeDcmStudyUidTable.GetMessages())
                    {
                        Messages.Add(msg);
                    }
                    throw new Exception("    *** This is a problem with the execute the DicomStudy Table *** ");
                }
                //更新狀態
                await UpdateStudyMaintainStatusToDatabase();
                //QC operation log
                OperationContext.SetParams(Data.ModifyUser, Data.ToStudyUID, "", GenerateQCMergeDesc());
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                //出現錯誤,則將已處理的檔案刪除
                //FailedToDeleteNewImageFiles();
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                OperationContext.WriteFailedRecord();
                throw new Exception(Message);
            }
            Result = OpResult.OpSuccess;
            //刪除舊有的Dicom檔案
            // AfterSuccessfulThenDeleteOldDcmFiles();
            Messages.Add("    *** Successful cross-study data image merge");
            OperationContext.WriteSuccessRecord();
            return true;
        }        
        /// <summary>
        /// 建立原始的檢查表格
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CreateOriginalStudyTable(string userid, string tableName, string studyUid)
        {
            try
            {
                List<PairDatas> pkeys = new()
                {
                    { new PairDatas { Name = "StudyInstanceUID", Value = studyUid } }                      
                };
                List<PairDatas> nkeys = new()
                {
                    { new PairDatas { Name = "PatientID", Value = string.Empty } },
                    { new PairDatas { Name = "StudyDate", Value = string.Empty } },
                    { new PairDatas { Name = "StudyTime", Value = string.Empty } },
                    { new PairDatas { Name = "ReferringPhysiciansName", Value = string.Empty } },
                    { new PairDatas { Name = "StudyID", Value = string.Empty } },
                    { new PairDatas { Name = "AccessionNumber", Value = string.Empty } },
                    { new PairDatas { Name = "StudyDescription", Value = string.Empty } },
                    { new PairDatas { Name = "Modality", Value = string.Empty } },
                    { new PairDatas { Name = "PerformingPhysiciansName", Value = string.Empty } },
                    { new PairDatas { Name = "NameofPhysiciansReading", Value = string.Empty } },
                    { new PairDatas { Name = "StudyStatus", Value = string.Empty } }
                };

                OriginalStudyTable = await DbQryService.BuildTable(tableName, pkeys, nkeys, userid)
                                            .GetDataAsync();                
            }
            catch(Exception ex)
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
        /// 建立原始的系列表格
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CreateOriginalSeriesTable(string userid, string tableName, string studyUid)
        {
            try
            {
                List<PairDatas> pkeys = new()
                {                    
                    { new PairDatas { Name = "StudyInstanceUID", Value = studyUid } }
                };
                List<PairDatas> nkeys = new()
                {
                    { new PairDatas { Name = "SeriesInstanceUID", Value = string.Empty } },
                    { new PairDatas { Name = "SeriesModality", Value = string.Empty } },
                    { new PairDatas { Name = "SeriesDate", Value = string.Empty } },
                    { new PairDatas { Name = "SeriesTime", Value = string.Empty } },
                    { new PairDatas { Name = "SeriesNumber", Value = string.Empty } },
                    { new PairDatas { Name = "SeriesDescription", Value = string.Empty } },                    
                    { new PairDatas { Name = "PatientPosition", Value = string.Empty } },
                    { new PairDatas { Name = "BodyPartExamined", Value = string.Empty } }
                };
                OriginalSeriesTable = await DbQryService.BuildTable(tableName, pkeys, nkeys, userid)
                                                .GetDataAsync();                
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
            // From 移除 Merge status
            DcmStudyQCStatusTable = new DicomStudyQCStatusTable(Data.ModifyUser);
            DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(Data.ToStudyUID, CommandFieldEvent.StudyMaintainType.Merged, 1);
            DbCmdService.TableElement = DcmStudyQCStatusTable;
            await DbCmdService.AddOrUpdate(true);
            // To 增加 Merge status，因為拆解是全拆解
            DcmStudyQCStatusTable = new DicomStudyQCStatusTable(Data.ModifyUser);
            DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(Data.FromStudyUID, CommandFieldEvent.StudyMaintainType.Merged, 0);
            DbCmdService.TableElement = DcmStudyQCStatusTable;
            await DbCmdService.AddOrUpdate(true);
        }

        /// <summary>
        /// 更新Dicom基本資訊(設定來自Appsetting)
        /// </summary>
        protected override bool ModifyDicomTag(DicomFile dcmFile, string newPatientId, string newStudyUID, string newSeriesUID, out string newImageUID)
        {
            try
            {
                var staticField = "[Static]";
                base.ModifyDicomTag(dcmFile, newPatientId, newStudyUID, newSeriesUID, out string newImgUID);

                List<PairDatas> pkeys = new()
                {
                    { new PairDatas { Name = "StudyInstanceUID", Value = newStudyUID } },
                    { new PairDatas { Name = "PatientId", Value = newPatientId } }
                };

                List<PairDatas> nkeys = MergeSplitMappingTagTable.Where(x=>x.Field != staticField).Select(mapper => new PairDatas { Name = mapper.Field, Value = string.Empty }).ToList();

                var table = DbQryService.BuildTable("DcmFindStudyLevelView", pkeys, nkeys).GetData();

                DicomDataset dcmDataset = dcmFile.Dataset;
                DicomOperatorHelper dicomOperator = new();

                string value = dicomOperator.GetDicomValueToStringWithGroupAndElem(dcmDataset, DicomTag.SpecificCharacterSet.Group,
                    DicomTag.SpecificCharacterSet.Element, false);
                bool isUtf8 = value.Contains("192");

                foreach (var fieldToDcmTagMap in MergeSplitMappingTagTable)
                {
                    var updateValue = "";
                    if(fieldToDcmTagMap.Field == staticField)
                    {
                        updateValue = fieldToDcmTagMap.Default;
                    }
                    else
                    {
                        var dbField = table.DBDatasets.First().First(x => x.FieldName == fieldToDcmTagMap.Field);
                        updateValue = dbField.Value;
                    }
                    dicomOperator.ConvertTagStringToUIntGE(fieldToDcmTagMap.Tag, out ushort t_group, out ushort t_element);
                    dicomOperator.WriteDicomValueInDataset(dcmDataset, new DicomTag(t_group, t_element), updateValue, isUtf8);
                }

                //先產生SOP Instance UID資料
                int instanceNumber = Convert.ToInt32(dicomOperator.GetDicomValueToString(dcmDataset, DicomTag.InstanceNumber, DicomVR.IS, false));
                newImageUID = newSeriesUID + "." + Convert.ToString(instanceNumber);
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
        /// 合併操作說明
        /// </summary>
        /// <returns></returns>
        protected string GenerateQCMergeDesc()
        {
            string patientid = TableElementHelper.FindFieldFromDataset(QueryUidStudyTable, "PatientId", 0).Value;
            string accessionNumber = TableElementHelper.FindFieldFromDataset(QueryUidStudyTable, "AccessionNumber", 0).Value;
            return $"From patient id: {patientid}, accession number: {accessionNumber}";
        }
        #endregion
    }
    #endregion
}
