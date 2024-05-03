using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dicom;
using ISoftViewerLibrary.Logic.Converter;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
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
                    // DcmRepository.DicomDatasets.Clear();
                    // DcmRepository.DicomDatasets.Add(CreateTestWLDataset());

                    foreach (var dataset in DcmRepository.DicomDatasets)
                    {
                        List<DcmTagData> queryResult = new();
                        converter.ConvertToDcmTagData(dataset).ForEach(x => queryResult.Add(x));
                        // var queryResult = converter.PopulateFromDataset(dataset);
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


        private DicomDataset CreateTestWLDataset()
        {
            var file = DicomFile.Open(@"C:\Users\Romeo\Desktop\test2.dcm");

            // DicomElement element = file.Dataset.GetDicomItem<DicomElement>(DicomTag.PatientName);
            // string[] encodings = { "Big5", "GB2312", "Shift-JIS", "Windows-1252", "ISO-8859-1" };
            // foreach (var encodingName in encodings)
            // {
            //     try
            //     {
            //         Encoding encoding = Encoding.GetEncoding(encodingName);
            //         string value = encoding.GetString(element.Buffer.Data);
            //         Console.WriteLine($"使用 {encodingName} 編碼解碼: {value}");
            //     }
            //     catch (Exception ex)
            //     {
            //         Console.WriteLine($"無法使用 {encodingName} 編碼解碼: {ex.Message}");
            //     }
            // }

            return file.Dataset;
        }

        #endregion
    }

    #endregion
}