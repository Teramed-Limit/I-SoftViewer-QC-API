using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Services
{
    #region QcMappingStudyCmdService

    /// <summary>
    /// 檢查批配對應的Dicom Dataset資料
    /// </summary>
    public class QcMappingMultiStudyCmdService : QcMappingBaseService<List<DataCorrection.V1.DcmTagData>>
    {
        public QcMappingMultiStudyCmdService(DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService, IDcmUnitOfWork dcmUnitOfWork,
            IDcmCqusDatasets dcmCqusDatasts, EnvironmentConfiguration publicConfig,
            IEnumerable<SvrConfigurationsV2> svrConfiguration) : base(dbQryService, dbCmdService, dcmUnitOfWork,
            dcmCqusDatasts, publicConfig, svrConfiguration)
        {
        }

        #region Methods

        /// <summary>
        /// 執行動作
        /// </summary>
        /// <returns></returns>
        public override async Task<bool> Execute()
        {
            try
            {
                Serilog.Log.Information("Start multi mapping study data and image");

                var index = 0;
                foreach (var dataset in Data.Dataset)
                {
                    // 先確定即將要被配對的Study是否已經存在
                    if (await DoseStudyExist(dataset))
                        throw new Exception("Study Instance UID duplicated.");

                    AssignPidNdStudyUid(dataset);

                    //取得要做Mapping的檢查,系列及影像
                    //因為來自於同一個PaitentId,所以第二個不需要去改資料庫
                    if (index == 0)
                    {
                        if (await QueryUidTable(Data.ModifyUser, Data.StudyInstanceUID, true) == false)
                            throw new Exception("Failed to query to mapping table or storage device data!!");
                    }

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

                        if (TobeDcmStudyUidTable.DetailElements[seIdx] is not DicomSeriesUniqueIdentifiersTable
                            _seTable)
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
                            if ((haveProcessed &= MappingDatasetToDcmFile(dataset, dcmFile, dcmHelper, index > 0)) ==
                                false)
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

                    //如果資料有異動才做處理
                    if (haveProcessed)
                    {
                        //先更新資料庫
                        //第二個Mapping不需要更新資料庫
                        if (needToUpdateDb && index == 0)
                            await UpdateDicomTableToDatabase();

                        //指派新的StudyInstanceUID
                        NewStudyInstanceUID = modifiedDcmFile.First().Value.Dataset
                            .GetSingleValue<string>(DicomTag.StudyInstanceUID);

                        //上傳Teramed PACS Service
                        if (await SendDcmToScp(modifiedDcmFile) == false)
                            throw new Exception(Message);

                        //更新狀態
                        await MakeSureStudyExist(NewStudyInstanceUID);
                        await UpdateStudyMaintainStatusToDatabase(NewStudyInstanceUID);
                        OperationContext.SetParams(Data.ModifyUser, NewStudyInstanceUID, "", "");
                        OperationContext.WriteSuccessRecord();
                    }

                    index++;
                }

                Result = OpResult.OpSuccess;
                Serilog.Log.Information("End mapping multi study data and image");
                return true;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                // OperationContext.WriteFailedRecord(ex.Message, ex.ToString());
                // PrintLog();
                Serilog.Log.Error(ex, "Mapping multi study data and image failed");
                throw new Exception(Message);
            }
        }

        #endregion
    }

    #endregion
}