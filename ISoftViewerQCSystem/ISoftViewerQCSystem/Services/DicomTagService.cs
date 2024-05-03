using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dicom;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerLibrary.Utils;
using Log = Serilog.Log;

namespace ISoftViewerQCSystem.Services
{
    /// <summary>
    ///     DicomTag CRUD
    /// </summary>
    public class DicomTagService
    {
        private readonly IDcmRepository _dcmRepository;
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly DicomOperatorHelper _dicomOperator = new();
        private readonly DicomPatientService _dicomPatientService;
        private readonly DicomStudyService _dicomStudyService;
        private readonly IDcmUnitOfWork _netUnitOfWork;
        private readonly QCOperationContext _qcOperationContext;

        private readonly List<EditableDicomTagData> _tagDataList;
        private List<string> _modifiableTagList;

        public DicomTagService(
            IDcmUnitOfWork netUnitOfWork,
            IDcmRepository dcmRepository,
            QCOperationContext qcOperationContext,
            DicomImagePathViewService dicomImagePathService,
            DicomPatientService dicomPatientService,
            DicomStudyService dicomStudyService)
        {
            _netUnitOfWork = netUnitOfWork;
            _dcmRepository = dcmRepository;
            _qcOperationContext = qcOperationContext;
            _dicomImagePathService = dicomImagePathService;
            _dicomPatientService = dicomPatientService;
            _dicomStudyService = dicomStudyService;
            _tagDataList = new List<EditableDicomTagData>();
        }

        /// <summary>
        ///     fo-dicom IDatasetWalker迭代Tag使用
        /// </summary>
        private void AddItem(int level, DicomTag tag, string name, string vr, string length, string value)
        {
            var editable = _modifiableTagList.Any(x =>
                string.Equals(x, tag.ToString(), StringComparison.CurrentCultureIgnoreCase));

            _tagDataList.Add(new EditableDicomTagData
            {
                Id = _tagDataList.Count.ToString(),
                Level = level,
                Tag = $"{tag.ToString().ToUpper()}",
                Group = tag.Group,
                Element = tag.Element,
                Name = name,
                VR = vr,
                Length = length,
                Value = value,
                Editable = level == 0 && editable
            });
        }

        /// <summary>
        ///     取得檔案所有Tag
        /// </summary>
        public List<EditableDicomTagData> GetDcmTag(string filePath, List<string> modifiableTagList)
        {
            _modifiableTagList = modifiableTagList;
            if (!File.Exists(filePath))
                return null;

            var dataset = DicomFile.Open(filePath).Dataset;

            new DicomDatasetWalker(dataset).Walk(new DatasetWalker(AddItem));
            return _tagDataList;
        }

        /// <summary>
        ///     修改Tag
        /// </summary>
        public async Task<bool> ModifyTag(
            string userName,
            string instanceUIDKey,
            string instanceUIDValue,
            ushort group,
            ushort element,
            string value,
            DicomOperationNodes dicomOperationNodes,
            bool createNewStudy = false)
        {
            var modifyDcmFiles = new List<string>();
            var dicomDictionary = new DicomTag(group, element).DictionaryEntry;
            var oriValue = "";
            var studyInsUid = "";
            try
            {
                var where = new List<PairDatas> { new() { Name = instanceUIDKey, Value = instanceUIDValue } };
                var imagePathList = _dicomImagePathService.Get(where);

                // 照常理，多個dcm檔都隸屬於同一個檢查
                foreach (var filePath in imagePathList.Select(x => x.ImageFullPath))
                {
                    if (!File.Exists(filePath))
                    {
                        Log.Error($"{filePath}, file not exist.");
                        continue;
                    }

                    var copyDcm = Path.Combine(Path.GetDirectoryName(filePath),
                        Path.GetFileNameWithoutExtension(filePath) + "_modifier" + ".dcm");

                    modifyDcmFiles.Add(copyDcm);

                    File.Copy(filePath, copyDcm, true);

                    // Modify Tag
                    var dataset = (await DicomFile.OpenAsync(copyDcm)).Dataset;
                    oriValue = dataset.GetString(new DicomTag(group, element));
                    var characterSet = _dicomOperator.GetDicomValueToStringWithGroupAndElem(dataset,
                        DicomTag.SpecificCharacterSet.Group,
                        DicomTag.SpecificCharacterSet.Element,
                        false);
                    var isUtf8 = characterSet.Contains("192");
                    _dicomOperator.WriteDicomValueInDataset(dataset, new DicomTag(group, element), value, isUtf8);
                    studyInsUid = dataset.GetString(DicomTag.StudyInstanceUID);

                    // Collect dataset
                    _dcmRepository.DicomDatasets.Add(dataset);

                    // Logging
                    Log.Information($"*** Tag Modification ***");
                    Log.Information($"StudyInstanceUID {dataset.GetString(DicomTag.StudyInstanceUID)}");
                    Log.Information($"SeriesInstanceUID {dataset.GetString(DicomTag.SeriesInstanceUID)}");
                    Log.Information($"SOPInstanceUID {dataset.GetString(DicomTag.SOPInstanceUID)}");
                    Log.Information($"Modify {dicomDictionary.Tag} from {oriValue} to {value}");
                }

                // 監測PatientId是否有改
                // 要先更新資料庫,PACS Server更新4層是同一個Transaction,會導致錯誤
                if (value != oriValue && dicomDictionary.Tag.ToString() == "(0010,0020)")
                {
                    UpdateDicomPatientToDatabase(oriValue, value);
                    UpdateDicomStudyToDatabase(oriValue, value, studyInsUid);
                }

                // 根據instanceUIDKey去記錄使用者操作
                RecordingTagOperation(instanceUIDKey, oriValue, value, studyInsUid, userName, dicomDictionary);

                //上傳Teramed PACS Service
                return await StorePacs(dicomOperationNodes);
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
                throw;
            }
            finally
            {
                foreach (var dcm in modifyDcmFiles) File.Delete(dcm);
            }
        }

        // private Task GenerateStudy(IEnumerable<string> modifyDcmFiles)
        // {
        //     // 列屬於同一個檢查
        //     // Group by SeriesInstanceUid
        //     var groupBySeriesInstanceUid =
        //         _dcmRepository.DicomDatasets.GroupBy(dataset => dataset.GetString(DicomTag.SeriesInstanceUID));
        //
        //     // 根據Grouping產生InstanceUID
        //     var studyInstanceUid = _accessionNumberGenerator.GenerateAccessionNumberAndInstanceUID().StudyInstanceUID;
        //
        //     var seriesIdx = 1;
        //     foreach (var seriesGroup in groupBySeriesInstanceUid)
        //     {
        //         var seriesInstanceUid = studyInstanceUid + "." + Convert.ToString(seriesIdx);
        //         var imageIdx = 1;
        //         foreach (var dataset in seriesGroup)
        //         {
        //             var sopInstanceUid = seriesInstanceUid + "." + Convert.ToString(imageIdx);
        //             _dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.StudyInstanceUID, studyInstanceUid,
        //                 false);
        //             _dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.SeriesInstanceUID, seriesInstanceUid,
        //                 false);
        //             _dicomOperator.WriteDicomValueInDataset(dataset, DicomTag.SOPInstanceUID, sopInstanceUid, false);
        //             _dcmRepository.DicomDatasets.Add(dataset);
        //             imageIdx++;
        //         }
        //
        //         seriesIdx++;
        //     }
        //
        //     return Task.CompletedTask;
        // }

        // 記錄使用者操作
        private void RecordingTagOperation(
            string type, string oriValue, string value, string studyInsUid, string userName,
            DicomDictionaryEntry dicomDictionary)
        {
            var desc = $"modify {dicomDictionary.Keyword}{dicomDictionary.Tag} from '{oriValue}' to '{value}'";
            switch (type)
            {
                case "StudyInstanceUID":
                    desc = $"Apply study's images, {desc}";
                    break;
                case "SeriesInstanceUID":
                    desc = $"Apply series's images, {desc}";
                    break;
                case "SOPInstanceUID":
                    desc = $"Apply image, {desc}";
                    break;
            }

            _qcOperationContext.SetLogger(new ModifyTagLogger());
            _qcOperationContext.SetParams(userName, studyInsUid, "", desc);
            _qcOperationContext.WriteSuccessRecord();
        }

        // Update DicomPatient
        private void UpdateDicomPatientToDatabase(string oriPatientId, string newPatientId)
        {
            var primaryFields = new List<PairDatas> { new() { Name = "PatientId", Value = oriPatientId } };
            var normalFields = new List<PairDatas> { new() { Name = "PatientId", Value = newPatientId } };
            var tableField = new TableField { PrimaryFields = primaryFields, NormalFields = normalFields };
            _dicomPatientService.GenerateNewTransaction();
            _dicomPatientService.AddOrUpdate(tableField);
        }

        // Update DicomStudy
        private void UpdateDicomStudyToDatabase(string oriPatientId, string newPatientId, string studyInstanceUID)
        {
            var primaryFields = new List<PairDatas>
            {
                new() { Name = "PatientId", Value = oriPatientId },
                // new() { Name = "StudyInstanceUID", Value = studyInstanceUID }
            };
            var normalFields = new List<PairDatas> { new() { Name = "PatientId", Value = newPatientId } };
            var tableField = new TableField { PrimaryFields = primaryFields, NormalFields = normalFields };
            _dicomStudyService.GenerateNewTransaction();
            _dicomStudyService.AddOrUpdate(tableField);
        }

        private async Task<bool> StorePacs(DicomOperationNodes dicomOperationNodes)
        {
            _netUnitOfWork.RegisterRepository(_dcmRepository);
            _netUnitOfWork.Begin(dicomOperationNodes.IPAddress, dicomOperationNodes.Port, dicomOperationNodes.AETitle,
                dicomOperationNodes.RemoteAETitle,
                Types.DcmServiceUserType.dsutStore
            );
            if (await _netUnitOfWork.Commit() == false)
                throw new Exception(_netUnitOfWork.Message);

            return true;
        }
    }
}