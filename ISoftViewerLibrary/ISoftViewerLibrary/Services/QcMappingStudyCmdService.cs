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
    public class QcMappingStudyCmdService : QcMappingBaseService<DataCorrection.V1.DcmTagData>
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
        public QcMappingStudyCmdService(DbQueriesService<CustomizeTable> dbQryService,
            DbCommandService<CustomizeTable> dbCmdService,
            IDcmUnitOfWork dcmUnitOfWork,
            IDcmCqusDatasets dcmCqusDatasts,
            EnvironmentConfiguration publicConfig,
            IEnumerable<SvrConfigurationsV2> svrConfiguration)
            : base(dbQryService, dbCmdService, dcmUnitOfWork, dcmCqusDatasts, publicConfig, svrConfiguration)
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
                Serilog.Log.Information("Start mapping study data and image");

                //確定即將要被配對的Study
                if (await DoseStudyExist(Data.Dataset))
                    throw new Exception("Study Instance UID duplicated.");

                //取得要做Mapping的檢查,系列及影像，之後要去更新資料庫用
                if (await QueryUidTable(Data.ModifyUser, Data.StudyInstanceUID, true) == false)
                    throw new Exception("Failed to query to mapping table or storage device data!!");

                if (await CEchoSCP() == false)
                    throw new Exception("Failed to connect to PACS server!!");

                //Mapping資料
                AssignPidNdStudyUid(Data.Dataset);

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
                    if (TobeDcmStudyUidTable.DetailElements[seIdx] is not DicomSeriesUniqueIdentifiersTable _seTable)
                        throw new Exception("        Illegal series table");

                    //Series Table關聯要先處理
                    string seriesUID = _seTable.SeriesInstanceUID.Value.Trim();
                    string newSeriesUID = _seTable.UpdateSeriesInstanceUID.Value.Trim();
                    _seTable.UpdateInstanceUIDAndData(newSeriesUID, NewStudyInstanceUID, Data.ModifyUser);
                    _seTable.UpdateKeyValueSwap();
                    //Mapping不會動到Image層的資料,所以資料庫不需要更新任何資料
                    for (int imIdx = 0; imIdx < _seTable.DetailElements.Count; imIdx++)
                    {
                        //先組合完整的檔案路徑
                        if (_seTable.DetailElements[imIdx] is not DicomImageUniqueIdentifiersTable _imgTable)
                            throw new Exception("        Illegal image table");

                        string dcmFilePath = string.Empty;
                        DicomFile dcmFile = GetDicomFile(_imgTable, dcmHelper, ref dcmFilePath);
                        //Mapping資料,只要有檔案不需要調整,則不繼續處理
                        if ((haveProcessed &= MappingDatasetToDcmFile(Data.Dataset, dcmFile, dcmHelper)) == false)
                            break;

                        // 更新Instance UID
                        dcmFile.Dataset.AddOrUpdate(DicomTag.StudyInstanceUID, NewStudyInstanceUID);
                        dcmFile.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID,
                            _seTable.UpdateSeriesInstanceUID.Value.Trim());
                        dcmFile.Dataset.AddOrUpdate(DicomTag.SOPInstanceUID,
                            _imgTable.UpdateSOPInstanceUID.Value.Trim());

                        modifiedDcmFile.Add(dcmFilePath, dcmFile);
                        _imgTable.UpdateKeyValueSwap();
                    }
                }

                //如果資料有異動才做處理
                if (haveProcessed)
                {
                    //先更新資料庫
                    if (needToUpdateDb == true)
                        await UpdateDicomTableToDatabase();
                    //上傳Teramed PACS Service
                    if (await SendDcmToScp(modifiedDcmFile) == false)
                        throw new Exception(Message);

                    //更新狀態
                    await UpdateStudyMaintainStatusToDatabase(NewStudyInstanceUID);
                    OperationContext.WriteSuccessRecord();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Messages.Add(ex.Message);
                Result = OpResult.OpFailure;
                // OperationContext.WriteFailedRecord(ex.Message, ex.StackTrace);
                Serilog.Log.Error(ex, "Mapping study data and image failed");
                PrintLog();
                throw new Exception(Message);
            }

            Serilog.Log.Information("End mapping study data and image");
            Result = OpResult.OpSuccess;
            PrintLog();
            return true;
        }

        #endregion
    }

    #endregion
}