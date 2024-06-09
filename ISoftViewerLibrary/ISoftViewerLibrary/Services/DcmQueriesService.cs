using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using Dicom.IO;
using ISoftViewerLibrary.Logic.Converter;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Utils;
using static ISoftViewerLibrary.Models.ValueObjects.Types;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerLibrary.Services
{
    #region DcmQueriesService

    /// <summary>
    ///     DICOM查詢服務
    /// </summary>
    public class DcmQueriesService : IDcmQueries
    {
        /// <summary>
        ///     建構
        /// </summary>
        /// <param name="netUnitOfWork"></param>
        /// <param name="dcmRepository"></param>
        /// <param name="dbQryService"></param>
        /// <param name="config"></param>
        public DcmQueriesService(
            IDcmUnitOfWork netUnitOfWork, IDcmRepository dcmRepository, DbQueriesService<CustomizeTable> dbQryService,
            EnvironmentConfiguration config)
        {
            NetUnitOfWork = netUnitOfWork;
            DcmRepository = dcmRepository;
            NetUnitOfWork.RegisterRepository(DcmRepository);
            DbQryService = dbQryService;
            EnvirConfig = config;

            Message = "";
            Result = OpResult.OpSuccess;
        }

        #region Fields

        /// <summary>
        ///     DICOM網路處理作業
        /// </summary>
        private readonly IDcmUnitOfWork NetUnitOfWork;

        /// <summary>
        ///     DICOM處理庫
        /// </summary>
        private readonly IDcmRepository DcmRepository;

        /// <summary>
        ///     Database查詢服務
        /// </summary>
        private readonly DbQueriesService<CustomizeTable> DbQryService;

        /// <summary>
        ///     環境組態
        /// </summary>
        private readonly EnvironmentConfiguration EnvirConfig;

        /// <summary>
        ///     訊息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///     處理結果
        /// </summary>
        public OpResult Result { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        ///     DICOM查詢服務
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        public async Task<Queries.V1.QueryResult> FindDataJson(
            DicomIODs dicomIODs, DcmServiceUserType type, string host, int port, string callingAe,
            string calledAe, Dictionary<string, object> parameter)
        {
            Queries.V1.QueryResult jsonDatasets = null;
            var retryTimes = 0;
            try
            {
                NetUnitOfWork.Begin(host, port, callingAe, calledAe, type, parameter);
                if (await DcmRepository.DcmDataEncapsulation(dicomIODs, type, parameter) == false)
                {
                    await NetUnitOfWork.Rollback();
                    throw new InvalidOperationException(DcmRepository.Message);
                }

                if (await NetUnitOfWork.Commit() == false)
                    throw new Exception(NetUnitOfWork.Message);

                DcmDatasetConvert2DcmTagData converter = new();
                if (type != DcmServiceUserType.dsutMove)
                {
                    jsonDatasets = new Queries.V1.QueryResult();
                    foreach (var dataset in DcmRepository.DicomDatasets)
                    {
                        List<DcmTagData> flatQueryResult = new();
                        // 這個是把所有Tag打平，供前端顯示使用
                        converter.ConvertToDcmTagData(dataset).ForEach(x => flatQueryResult.Add(x));
                        jsonDatasets.FlatDatasets.Add(flatQueryResult);
                        // 這個是原始的Dataset，供後端處理使用
                        var queryResult = converter.PopulateFromDataset(dataset);
                        jsonDatasets.Datasets.Add(queryResult);
                    }
                }
                else
                {
                    await Task.Delay(2000);
                    // PACS Server歸檔比C-Move還慢時會出錯
                    jsonDatasets = await FindDataJson(dicomIODs, DbSearchResultType.dsrSearchImagePath);
                    // 延遲幾秒載查一次
                    while (jsonDatasets.FileSetIDs.Count == 0 && retryTimes <= 5)
                    {
                        await Task.Delay(2000);
                        jsonDatasets = await FindDataJson(dicomIODs, DbSearchResultType.dsrSearchImagePath);
                        retryTimes++;
                    }
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                throw;
            }

            return await Task.FromResult(jsonDatasets);
        }

        /// <summary>
        ///     查詢資料庫資料
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<Queries.V1.QueryResult> FindDataJson(DicomIODs dicomIODs, DbSearchResultType type)
        {
            try
            {
                Queries.V1.QueryResult result = new();
                string tableName = GetEnumDescription(type);
                if (tableName == string.Empty)
                    throw new Exception("This type of query is not supported");

                List<PairDatas> pKeys = new();
                List<PairDatas> nKeys = new();

                switch (type)
                {
                    case DbSearchResultType.dsrSearchImagePath:
                        if (CreateSearchImagePathKeys(dicomIODs, ref pKeys, ref nKeys) == false)
                            throw new Exception("Invalid query primary condition");

                        var dataset = await DbQryService.BuildTable(tableName, pKeys, nKeys)
                            .GetDataAsync();
                        dataset.DBDatasets.ForEach(fields =>
                        {
                            string filePath = fields.Find(fe => fe.FieldName == "HttpFilePath").Value
                                .Replace(@"\", "/");
                            string imageFullPath = EnvirConfig.VirtualFilePath + filePath;
                            result.FileSetIDs.Add(new DcmTagData
                            {
                                Group = DicomTag.FileSetID.Group,
                                Elem = DicomTag.FileSetID.Element,
                                Value = imageFullPath
                            });
                        });
                        break;
                }

                return await Task.FromResult(result);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        ///     建立查詢影像路徑主鍵
        /// </summary>
        /// <param name="studyInsUID"></param>
        /// <param name="seriesInsUID"></param>
        /// <returns></returns>
        protected static bool CreateSearchImagePathKeys(
            DicomIODs dicomIODs, ref List<PairDatas> pkeys, ref List<PairDatas> nkeys)
        {
            if (pkeys == null || nkeys == null)
                return false;

            if (dicomIODs.Studies.Any())
            {
                string studyInsUID = dicomIODs.Studies.First().StudyInstanceUID;
                if (studyInsUID != string.Empty)
                    pkeys.Add(new PairDatas { Name = "StudyInstanceUID", Value = studyInsUID });
            }

            if (dicomIODs.Series.Any())
            {
                var seriesInsUIDList = dicomIODs.Series.SelectMany(x => x.Value)
                    .Select(x => x.SeriesInstanceUID.Value)
                    .ToList()
                    .Aggregate((x, y) => $"'{x}','{y}'");

                if (seriesInsUIDList != string.Empty)
                    pkeys.Add(new PairDatas
                        { Name = "SeriesInstanceUID", Value = seriesInsUIDList, OperatorType = FieldOperator.foIn });
            }

            nkeys.Add(new PairDatas { Name = "HttpFilePath" });

            return pkeys.Any() && nkeys.Any();
        }


        private DicomDataset CreateTestDataset(int numnber)
        {
            var dataset = new DicomDataset()
            {
                EncodeValue(DicomTag.SpecificCharacterSet, "ISO_IR 100"),
                EncodeValue(DicomTag.AccessionNumber, "55397"+ "_" + numnber),
                EncodeValue(DicomTag.ReferringPhysicianName, "³¯¥ß²»"),
                new DicomSequence(DicomTag.ReferencedStudySequence,
                    new DicomDataset
                    {
                        EncodeValue(DicomTag.ReferencedSOPClassUID,
                            "1.2.410.200010.1140227.4636.1161209.1363005.1.1363005"),
                        EncodeValue(DicomTag.ReferencedSOPInstanceUID,
                            "1.2.410.200010.1140227.4636.1161209.1363005.1.1363005")
                    }),
                new DicomSequence(DicomTag.ReferencedPatientSequence,
                    new DicomDataset
                    {
                        EncodeValue(DicomTag.ReferencedSOPClassUID, "1.2.840.10008.3.1.2.3.1"),
                        EncodeValue(DicomTag.ReferencedSOPInstanceUID, "1.2.840.10008.3.1.2.3.1")
                    }),
                EncodeValue(DicomTag.PatientName, "±i¶À¤ë"),
                EncodeValue(DicomTag.PatientID, "10052351"),
                EncodeValue(DicomTag.PatientBirthDate, "19430125"),
                EncodeValue(DicomTag.PatientSex, "F"),
                EncodeValue(DicomTag.OtherPatientIDsRETIRED, "10052351"),
                EncodeValue(DicomTag.OtherPatientNames, "±i¶À¤ë"),
                // 這個要轉，原本是81Y
                EncodeValue(DicomTag.PatientAge, "081Y"),
                EncodeValue(DicomTag.PatientSize, ""),
                EncodeValue(DicomTag.PatientWeight, ""),
                EncodeValue(DicomTag.MedicalAlerts, ""),
                EncodeValue(DicomTag.Allergies, ""),
                EncodeValue(DicomTag.PregnancyStatus, ""),
                // EncodeValue(DicomTag.StudyInstanceUID, "1.2.410.200010.1140227.4636.1161209.1363005.1.1363005"),
                EncodeValue(DicomTag.StudyInstanceUID, DicomUID.Generate().UID),
                EncodeValue(DicomTag.RequestingPhysician, "³¯¥ß²»"),
                EncodeValue(DicomTag.RequestingService, "02"),
                EncodeValue(DicomTag.RequestedProcedureDescription, "¦µ¤R°Ç¤ó¤ßÅ¦¦å¬y¹Ï Doppler color flow mapping"),
                new DicomSequence(DicomTag.RequestedProcedureCodeSequence,
                    new DicomDataset
                    {
                        EncodeValue(DicomTag.CodeValue, "18007"),
                        EncodeValue(DicomTag.CodingSchemeDesignator, "802PT"),
                        EncodeValue(DicomTag.CodeMeaning, "¦µ¤R°Ç¤ó¤ßÅ¦¦å¬y¹Ï Doppler color flow mapping")
                    }),
                EncodeValue(DicomTag.AdmissionID, "1003730"),
                EncodeValue(DicomTag.SpecialNeeds, ""),
                EncodeValue(DicomTag.CurrentPatientLocation, "O"),
                EncodeValue(DicomTag.PatientInstitutionResidence, "W5-51"),
                EncodeValue(DicomTag.PatientState, ""),
                new DicomSequence(DicomTag.ScheduledProcedureStepSequence,
                    new DicomDataset
                    {
                        EncodeValue(DicomTag.Modality, "US"),
                        EncodeValue(DicomTag.RequestedContrastAgent, ""),
                        EncodeValue(DicomTag.ScheduledStationAETitle, "US"),
                        EncodeValue(DicomTag.ScheduledProcedureStepStartDate, "20240522"),
                        EncodeValue(DicomTag.ScheduledProcedureStepStartTime, "140100"),
                        EncodeValue(DicomTag.ScheduledPerformingPhysicianName, ""),
                        EncodeValue(DicomTag.ScheduledProcedureStepDescription,
                            "¦µ¤R°Ç¤ó¤ßÅ¦¦å¬y¹Ï Doppler color flow mapping"),
                        new DicomSequence(DicomTag.ScheduledProtocolCodeSequence,
                            new DicomDataset
                            {
                                EncodeValue(DicomTag.CodeValue, "18007"),
                                EncodeValue(DicomTag.CodingSchemeDesignator, "802PT"),
                                EncodeValue(DicomTag.CodingSchemeVersion, ""),
                                EncodeValue(DicomTag.CodeMeaning, "¦µ¤R°Ç¤ó¤ßÅ¦¦å¬y¹Ï Doppler color flow mapping")
                            }),
                        EncodeValue(DicomTag.ScheduledProcedureStepID, "696704"),
                        EncodeValue(DicomTag.ScheduledStationName, ""),
                        EncodeValue(DicomTag.ScheduledProcedureStepLocation, ""),
                        EncodeValue(DicomTag.PreMedication, ""),
                        EncodeValue(DicomTag.ScheduledProcedureStepStatus, "A"),
                    }),
                EncodeValue(DicomTag.RequestedProcedureID, "1363005"),
                EncodeValue(DicomTag.IssueDateOfImagingServiceRequest, "20240522"),
                EncodeValue(DicomTag.IssueTimeOfImagingServiceRequest, "140227"),
                EncodeValue(DicomTag.ConfidentialityConstraintOnPatientDataDescription, "Y")
            };


            static DicomItem EncodeValue(DicomTag tag, string value, string fromEncoding = "latin1",
                string toEncoding = "big5")
            {
                DicomItem[] items = new DicomItem[1];

                byte[] bytes = Encoding.GetEncoding(fromEncoding).GetBytes(value);
                string unicodeString = Encoding.GetEncoding(toEncoding).GetString(bytes);
                items[0] = new DicomLongString(tag, Encoding.GetEncoding(toEncoding), new[] { unicodeString });
                return items[0];
            }

            return dataset;
        }

        #endregion
    }

    #endregion
}