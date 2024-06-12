using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Dicom;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;

namespace ISoftViewerLibrary.Services;

public abstract class QcMappingBaseService<T> : QcStudyCmdWithDcmNetService<T> where T : class
{
    protected QcMappingBaseService(
        DbQueriesService<CustomizeTable> dbQryService,
        DbCommandService<CustomizeTable> dbCmdService,
        IDcmUnitOfWork dcmUnitOfWork,
        IDcmCqusDatasets dcmCqusDatasts,
        EnvironmentConfiguration publicConfig,
        IEnumerable<SvrConfigurationsV2> svrConfiguration)
        : base(dbQryService, dbCmdService, dcmUnitOfWork, dcmCqusDatasts, publicConfig, svrConfiguration)
    {
        //取得DICOM Tag對應資料
        var mappingTableJsonStr = svrConfiguration
            .ToList()
            .FirstOrDefault(x => x.SysConfigName == "QCMappingTable")
            ?.Value;

        // Validate the mapping table
        if (string.IsNullOrWhiteSpace(mappingTableJsonStr))
            throw new Exception("Mapping table is empty.");

        MappingTagDataset = JsonSerializer.Deserialize<MappingTagTable>(mappingTableJsonStr);
        NewPatientID = "";
        NewStudyInstanceUID = "";
    }

    #region Fields

    /// <summary>
    /// 註冊資料
    /// </summary>
    protected DataCorrection.V1.StudyMappingParameter<T> Data;

    /// <summary>
    /// DICOM tag mapping table字串
    /// </summary>
    protected MappingTagTable MappingTagDataset;

    /// <summary>
    /// QC操作紀錄
    /// </summary>
    protected QCOperationContext OperationContext { get; set; }

    #endregion


    /// <summary>
    /// 註冊資料
    /// </summary>
    /// <param name="data"></param>
    public override async Task RegistrationData(object data)
    {
        Data = (DataCorrection.V1.StudyMappingParameter<T>)data;

        // 獲取本地C-Store Node
        QueryLocalCStoreNode();

        // 先確定要配對過去的 Study 是否已經存在
        if (!await DoseStudyExist(Data.StudyInstanceUID))
            throw new Exception("Study Instance UID duplicated.");

        // 確定是否拿的檔案
        if (!await QueryStorageDevice())
            throw new Exception("Failed to query storage device data!!");
    }

    /// <summary>
    /// 註冊資料
    /// </summary>
    /// <param name="data"></param>
    protected void AssignPidNdStudyUid(List<DataCorrection.V1.DcmTagData> data)
    {
        //先取得PatientID & StudyInstanceUID
        data?.ForEach(data =>
        {
            if (data.Group == DicomTag.PatientID.Group && data.Elem == DicomTag.PatientID.Element)
                NewPatientID = data.Value;
            if (data.Group == DicomTag.StudyInstanceUID.Group && data.Elem == DicomTag.StudyInstanceUID.Element)
                NewStudyInstanceUID = data.Value;
        });

        OriginalPatientID = Data.PatientId;
        OriginalStudyInstanceUID = Data.StudyInstanceUID;

        Serilog.Log.Information(
            "Mapping OriginalPatientID: {OriginalPatientID}, OriginalStudyInstanceUID: {OriginalStudyInstanceUID}, NewPatientID: {NewPatientID}, NewStudyInstanceUID: {NewStudyInstanceUID}",
            OriginalPatientID, OriginalStudyInstanceUID, NewPatientID, NewStudyInstanceUID);
    }

    /// <summary>
    /// 註冊Study操作資料
    /// </summary>
    /// <param name="operationContext"></param>
    public override void RegistrationOperationContext(QCOperationContext operationContext)
    {
        OperationContext = operationContext;
        OperationContext.SetLogger(new MappingStudyLogger());
        OperationContext.SetParams(Data.ModifyUser, Data.StudyInstanceUID, "", "");
    }

    /// <summary>
    /// 更新檢查層Table
    /// </summary>
    /// <returns></returns>
    protected override async Task UpdateDicomTableToDatabase()
    {
        // if (NeedUpdatePatientTable == true)
        //     DcmPatientUidTable.UpdatePatientId(NewPatientID, Data.ModifyUser);

        string originalUid = TobeDcmStudyUidTable.StudyInstanceUID.Value.Trim();
        TobeDcmStudyUidTable
            .UpdateUpdateInstanceUID(insUid: originalUid, updateUID: NewStudyInstanceUID, Data.ModifyUser)
            .SetPatientId(NewPatientID);

        await base.UpdateDicomTableToDatabase();
    }

    /// <summary>
    /// 將外部資料更新到DICOM檔案
    /// </summary>
    /// <param name="dcmFile"></param>
    /// <param name="dcmHelper"></param>
    /// <returns></returns>
    protected bool MappingDatasetToDcmFile(
        List<DataCorrection.V1.DcmTagData> dataset,
        DicomFile dcmFile,
        DicomOperatorHelper dcmHelper,
        bool generateNewUid = false)
    {
        bool result = false;
        try
        {
            DicomDataset dcmFileDataset = dcmFile.Dataset;
            string value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dcmFileDataset,
                DicomTag.SpecificCharacterSet.Group, DicomTag.SpecificCharacterSet.Element, false);
            bool isUtf8 = value.Contains("192");

            MappingTagDataset.Dataset.ForEach(mappingDataset =>
            {
                // Data.Dataset已經是Worklist的資料
                var mapTagData = ConvertMapTag2DcmTag(dataset, mappingDataset, dcmHelper);
                // 找不到就不要處理了
                if (mapTagData == null) return;

                AddDcmTagToDataset(mapTagData, dcmFileDataset, dcmHelper, isUtf8);

                result = true;
            });

            if (generateNewUid)
            {
                dcmFileDataset.AddOrUpdate(DicomTag.StudyInstanceUID, NewStudyInstanceUID);
                dcmFileDataset.AddOrUpdate(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
                dcmFileDataset.AddOrUpdate(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());
            }
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
    /// 更新Study QC操作狀態
    /// </summary>
    /// <returns></returns>
    protected async Task UpdateStudyMaintainStatusToDatabase(string studyInstanceUID)
    {
        DcmStudyQCStatusTable = new DicomStudyQCStatusTable("");
        DcmStudyQCStatusTable.SetInstanceUIDAndMaintainType(studyInstanceUID,
            CommandFieldEvent.StudyMaintainType.Mapped, 1);
        DbCmdService.TableElement = DcmStudyQCStatusTable;

        var result = await DbCmdService.AddOrUpdate(true);
        if (result == false)
            throw new Exception("Failed to update study QC status to database!!");
    }

    /// <summary>
    /// 將外部資料更新到DICOM檔案
    /// </summary>s
    /// <param name="mappingDataset"></param>
    /// <param name="dcmHelper"></param>
    /// <returns></returns>
    public DataCorrection.V1.DcmTagData ConvertMapTag2DcmTag(
        List<DataCorrection.V1.DcmTagData> dataset,
        MappingTag mappingDataset,
        DicomOperatorHelper dcmHelper)
    {
        try
        {
            var fromTagValue = dcmHelper.GetDicomValueToStringWithDcmTagData(dataset, mappingDataset.FromTag);
            fromTagValue = string.IsNullOrEmpty(mappingDataset.Value) ? fromTagValue : mappingDataset.Value;

            // 如果 FromTag 是Seq內的Tag，直接從 Dataset 取值
            if (mappingDataset.FromTag.Contains("|"))
            {
                return CreateDcmTagData(mappingDataset.ToTag, fromTagValue, dcmHelper);
            }

            dcmHelper.ConvertTagStringToUIntGE(mappingDataset.FromTag, out ushort fGroup, out ushort fElement);
            var fromDicomTag = new DicomTag(fGroup, fElement);

            if (dcmHelper.IsSequenceTag(fromDicomTag))
            {
                if (mappingDataset.ToTag.Contains("|"))
                    throw new Exception($"ToTag: {mappingDataset.ToTag} cannot be a sequence.");

                var toDicomTag = dcmHelper.ConvertStringToDicomTag(mappingDataset.ToTag);
                return HandleSequenceTag(dataset, dcmHelper, fromDicomTag, toDicomTag);
            }

            // 兩個都不是 sequence，直接拿值
            return CreateDcmTagData(mappingDataset.ToTag, fromTagValue, dcmHelper);
        }
        catch (Exception e)
        {
            Serilog.Log.Error(e, "ConvertMapTag2DcmTag error");
            throw;
        }
    }

    private DataCorrection.V1.DcmTagData CreateDcmTagData(string tagStr, string value,
        DicomOperatorHelper dcmHelper)
    {
        // Create the root DcmTagData object
        var rootTag = CreateTag(tagStr.Split('|')[0], value, dcmHelper);

        // Split the tag string into individual tags
        var dicomTagList = tagStr.Split('|');

        // Loop through each tag string and create corresponding DcmTagData objects
        for (int i = 1; i < dicomTagList.Length; i++)
        {
            var currentTag = CreateTag(dicomTagList[i], value, dcmHelper);
            rootTag.SeqDcmTagData ??= new List<DataCorrection.V1.DcmTagData>();
            rootTag.SeqDcmTagData.Add(currentTag);
        }

        return rootTag;
    }

    // Helper method to create a DcmTagData object from a tag string
    private DataCorrection.V1.DcmTagData CreateTag(string tagStr, string value, DicomOperatorHelper dcmHelper)
    {
        var tag = dcmHelper.ConvertStringToDicomTag(tagStr);
        if (dcmHelper.IsSequenceTag(tag)) value = string.Empty;
        return new DataCorrection.V1.DcmTagData
        {
            Keyword = tag.DictionaryEntry.Keyword,
            Name = tag.ToString(),
            Group = tag.Group,
            Elem = tag.Element,
            Value = value,
            SeqDcmTagData = new List<DataCorrection.V1.DcmTagData>(),
        };
    }

    private DataCorrection.V1.DcmTagData HandleSequenceTag(
        List<DataCorrection.V1.DcmTagData> dataset,
        DicomOperatorHelper dcmHelper,
        DicomTag fromDicomTag,
        DicomTag toDicomTag)
    {
        if (!dcmHelper.IsSequenceTag(toDicomTag))
        {
            throw new Exception("FromTag is sequence, but ToTag is not sequence.");
        }

        var currentTagData =
            dataset.Find(tag => tag.Group == fromDicomTag.Group && tag.Elem == fromDicomTag.Element);
        if (currentTagData?.SeqDcmTagData == null)
        {
            Serilog.Log.Error("FromTag is sequence, but currentTagData or SeqDcmTagData is null", fromDicomTag);
            return null;
        }

        return new DataCorrection.V1.DcmTagData
        {
            Keyword = toDicomTag.DictionaryEntry.Keyword,
            Name = toDicomTag.ToString(),
            Group = toDicomTag.Group,
            Elem = toDicomTag.Element,
            Value = "",
            SeqDcmTagData = currentTagData.SeqDcmTagData,
        };
    }

    /// <summary>
    /// 依據Table內容去取得DicomFile物件
    /// </summary>
    /// <param name="imgUidTable"></param>
    /// <returns></returns>
    protected override DicomFile GetDicomFile(
        DicomImageUniqueIdentifiersTable imgUidTable,
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
            MappingTagDataset.Dataset.Add(new MappingTag
            {
                FromTag = "0020,000e",
                ToTag = "0020,000e",
                Value = dcmFile.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID)
            });
            MappingTagDataset.Dataset.Add(new MappingTag
            {
                FromTag = "0020,000d",
                ToTag = "0020,000d",
                Value = dcmFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)
            });
            MappingTagDataset.Dataset.Add(new MappingTag
            {
                FromTag = "0008,0018",
                ToTag = "0008,0018",
                Value = dcmFile.Dataset.GetSingleValue<string>(DicomTag.SOPInstanceUID)
            });
            string unmappedTagTable = GetUnmappedDcmTags(dcmFile, MappingTagDataset, dcmHelper);
            imgUidTable.UpdateUnmappedDcmTag(unmappedTagTable);
        }

        // 複製一份原始檔案
        var modDcmFile = dcmFile.Clone();

        return modDcmFile;
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
            DicomDataset dcmFileDataset = dicomFile.Dataset;

            //先判斷是什麼編碼
            string value = dcmHelper.GetDicomValueToStringWithGroupAndElem(dcmFileDataset,
                DicomTag.SpecificCharacterSet.Group, DicomTag.SpecificCharacterSet.Element, false);
            bool isUtf8 = value.Contains("192");

            List<DataCorrection.V1.DcmTagData> dcmTagDatas = new();
            dcmTagDatas =
                dcmHelper.ConvertDicomDatasetToDcmTagDataList(dcmFileDataset, tagTable.Dataset, dcmHelper, isUtf8);

            if (dcmTagDatas.Any())
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

    private void AddDcmTagToDataset(
        DataCorrection.V1.DcmTagData mapTagData,
        DicomDataset dataset,
        DicomOperatorHelper dcmHelper,
        bool isUtf8)
    {
        // 創建 DicomTag 物件，使用群組和元素來識別標籤
        DicomTag dicomTag = new DicomTag((ushort)mapTagData.Group, (ushort)mapTagData.Elem);

        // 判斷該標籤是否為序列類型
        bool isSequence = dcmHelper.IsSequenceTag(dicomTag);

        if (isSequence)
        {
            // 建立 DicomSequence 物件，或取得已存在的序列
            dataset.TryGetSequence(dicomTag, out DicomSequence sequence);
            sequence ??= new DicomSequence(dicomTag);

            // 建立一個新的 DicomDataset 作為序列項目
            var sequenceItem = new DicomDataset();
            // 如果序列中已有項目，則取第一個項目
            if (sequence.Items.Any())
                sequenceItem = sequence.Items.First();

            // 對每個子標籤數據進行處理
            foreach (var subTagData in mapTagData.SeqDcmTagData)
            {
                AddDcmTagToDataset(subTagData, sequenceItem, dcmHelper, isUtf8);
            }

            // 如果序列項目列表為空，則添加新的序列項目
            if (!sequence.Items.Any())
                sequence.Items.Add(sequenceItem);

            // 更新數據集中的序列
            dataset.AddOrUpdate(sequence);
        }
        // 非序列，直接寫入值
        else
        {
            // 取得標籤的原始值
            var originalValue =
                dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, dicomTag.Group, dicomTag.Element, isUtf8);

            // 如果原始值與新值不同，則寫入新值
            if (originalValue != mapTagData.Value)
            {
                dcmHelper.WriteDicomValueInDataset(dataset, dicomTag, mapTagData.Value, isUtf8);
            }
        }
    }

    protected Task<bool> DoseStudyExist(List<DataCorrection.V1.DcmTagData> dataset)
    {
        // 即將要被Mapping的StudyInstanceUID，不能有重複
        var studyInstanceUID = dataset.First(x =>
            x.Group == DicomTag.StudyInstanceUID.Group &&
            x.Elem == DicomTag.StudyInstanceUID.Element).Value;

        return DoseStudyExist(studyInstanceUID);
    }

    /// <summary>
    /// 執行動作
    /// </summary>
    /// <returns></returns>
    public abstract override Task<bool> Execute();
}