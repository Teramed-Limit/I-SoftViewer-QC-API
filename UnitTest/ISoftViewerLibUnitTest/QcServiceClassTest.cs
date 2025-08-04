using Dicom;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class QcServiceClassTest
    {
        // [TestMethod]
        // public async Task T1_MergeStudy_Result_AreEqual()
        // {
        //     DataCorrection.V1.MergeStudyParameter parameter = new()
        //     {
        //         ModifyUser = "UnitTester",
        //         FromStudyUID = "1.3.6.1.4.1.29974.84.20211206.205213.102467",
        //         ToStudyUID = "1.3.6.1.4.1.29974.84.20211206.205205.102388"
        //     };
        //     //ADD 20220323 Oscar add config for merge object
        //     MappingTagTable tjson = new();
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0010", ToTag = "0010,0010" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0020", ToTag = "0010,0020" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0030", ToTag = "0010,0030" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0040", ToTag = "0010,0040" });
        //
        //     EnvironmentConfiguration config = new()
        //     {
        //         CalledAeTitle = "SCP202",
        //         CallingAeTitle = "SCU202",
        //         DcmSendIP = "192.168.1.8",
        //         DcmSendPort = 202,
        //         DcmTagMappingTable = tjson
        //     };
        //
        //     DbQueriesService<CustomizeTable> dbQueriesService = new("sa", "oscar", "PACSServer", "tcp:localhost");
        //     DbCommandService<CustomizeTable> dbCmdService = new("sa", "oscar", "PACSServer", "tcp:localhost", false);
        //     QcMergeStudyCmdService mergeStudyCmd = new(dbQueriesService, dbCmdService, config);
        //     mergeStudyCmd.RegistrationData(parameter);
        //     var result = await mergeStudyCmd.Execute();
        //     Assert.AreEqual(true, result);
        // }
        //
        // [TestMethod]
        // public async Task T2_SplitStudy_Result_AreEqual()
        // {
        //     DataCorrection.V1.SplitStudyParameter parameter = new()
        //     {
        //         ModifyUser = "UnitTester",
        //         StudyInstanceUID = "1.3.6.1.4.1.29974.84.20211206.205205.102388",
        //         AfterSplitStudyToDeleteOldFiles = true
        //     };
        //     DbQueriesService<CustomizeTable> dbQueriesService = new("sa", "oscar", "PACSServer", "tcp:localhost");
        //     DbCommandService<CustomizeTable> dbCmdService = new("sa", "oscar", "PACSServer", "tcp:localhost", false);
        //
        //     //ADD 20220323 Oscar add config for split object
        //     MappingTagTable tjson = new();
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0010", ToTag = "0010,0010" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0020", ToTag = "0010,0020" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0030", ToTag = "0010,0030" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0040", ToTag = "0010,0040" });
        //
        //     EnvironmentConfiguration config = new()
        //     {
        //         CalledAeTitle = "SCP202",
        //         CallingAeTitle = "SCU202",
        //         DcmSendIP = "192.168.1.8",
        //         DcmSendPort = 202,
        //         DcmTagMappingTable = tjson
        //     };
        //
        //     QcSplitStudyCmdService qcSplitStudyCmd = new(dbQueriesService, dbCmdService, config);
        //     qcSplitStudyCmd.RegistrationData(parameter);
        //     var result = await qcSplitStudyCmd.Execute();
        //
        //     Assert.AreEqual(true, result);
        // }
        //
        // public void Test()
        // {
        //     MappingTagTable tjson = new();
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0010", ToTag = "0010,0010" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0020", ToTag = "0010,0020" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0030", ToTag = "0010,0030" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0040", ToTag = "0010,0040" });
        //
        //     //var options = new JsonSerializerOptions { WriteIndented = true };
        //     //string jsonString = JsonSerializer.Serialize(tjson, options);
        //
        //     string jsonString = JsonSerializer.Serialize(tjson);
        //
        //     string json = "{\"Dataset\":[{\"FromTag\":\"0020,0020\",\"ToTag\":\"0020,0020\"}, {\"FromTag\":\"0020,0030\",\"ToTag\":\"0020,0030\"}, {\"FromTag\":\"0020,0040\",\"ToTag\":\"0020,0040\"}]}";            
        //     try
        //     {                
        //         MappingTagTable Dataset = JsonSerializer.Deserialize<MappingTagTable>(json);
        //     }
        //     catch(Exception ex)
        //     {
        //         string message = ex.Message;
        //     }            
        // }
        //
        // [TestMethod]
        // public async Task T3_MappingStudy_Result_AreEqual()
        // {   
        //     DbQueriesService<CustomizeTable> dbQueriesService = new("sa", "victor70394219", "PACSServer", "tcp:localhost");
        //     DbCommandService<CustomizeTable> dbCmdService = new("sa", "victor70394219", "PACSServer", "tcp:localhost", false);
        //     IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
        //     IDcmCqusDatasets dcmRepository = new DcmSendServiceHelper();
        //
        //     MappingTagTable tjson = new();
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0010", ToTag = "0010,0010" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0020", ToTag = "0010,0020" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0030", ToTag = "0010,0030" });
        //     tjson.Dataset.Add(new MappingTag() { FromTag = "0010,0040", ToTag = "0010,0040" });
        //     
        //     EnvironmentConfiguration config = new()
        //     {
        //         CalledAeTitle = "SCP104",
        //         CallingAeTitle = "SCU1",
        //         DcmSendIP = "192.168.1.2",
        //         DcmSendPort = 104,
        //         DcmTagMappingTable = tjson  //Modify Oscar 20220323 just assign, don't need serialize
        //     };
        //
        //     DataCorrection.V1.StudyMappingParameter mappingParameter = new()
        //     {
        //         ModifyUser = "Mapping Study User",
        //         PatientId = "P20211206099773",
        //         StudyInstanceUID = "1.3.6.1.4.1.29974.84.20211206.205205.102388"
        //     };
        //     mappingParameter.Dataset.Add(new DataCorrection.V1.DcmTagData()
        //     { Group = DicomTag.PatientName.Group, Elem = DicomTag.PatientName.Element, Value = "UnitTestPatientName" });
        //     mappingParameter.Dataset.Add(new DataCorrection.V1.DcmTagData()
        //     { Group = DicomTag.PatientID.Group, Elem = DicomTag.PatientID.Element, Value = "UnitTestPatientID" });
        //     mappingParameter.Dataset.Add(new DataCorrection.V1.DcmTagData()
        //     { Group = DicomTag.PatientSex.Group, Elem = DicomTag.PatientSex.Element, Value = "M" });
        //     mappingParameter.Dataset.Add(new DataCorrection.V1.DcmTagData()
        //     { Group = DicomTag.PatientBirthDate.Group, Elem = DicomTag.PatientBirthDate.Element, Value = "20001212" });
        //
        //     IAsyncCommandExecutor mappingCmd = new QcMappingStudyCmdService(dbQueriesService, dbCmdService, netUnitOfWork,
        //         dcmRepository, config);
        //     mappingCmd.RegistrationData(mappingParameter);
        //     var result = await mappingCmd.Execute();
        //
        //     Assert.AreEqual(true, result);
        // }
        //
        // [TestMethod]
        // public async Task T4_UnmappingStudy_Result_AreEqual()
        // {
        //     DbQueriesService<CustomizeTable> dbQueriesService = new("sa", "victor70394219", "PACSServer", "tcp:localhost");
        //     DbCommandService<CustomizeTable> dbCmdService = new("sa", "victor70394219", "PACSServer", "tcp:localhost", false);
        //     IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
        //     IDcmCqusDatasets dcmRepository = new DcmSendServiceHelper();
        //
        //     EnvironmentConfiguration config = new()
        //     {
        //         CalledAeTitle = "SCP104",
        //         CallingAeTitle = "SCU1",
        //         DcmSendIP = "192.168.1.2",
        //         DcmSendPort = 104                
        //     };
        //
        //     DataCorrection.V1.StudyUnmappingParameter unmappingParameter = new()
        //     {
        //         ModifyUser = "Umapping Study Tester",
        //         PatientId = "UnitTestPatientID",
        //         StudyInstanceUID = "1.3.6.1.4.1.29974.84.20211206.205205.102388"
        //     };
        //
        //     IAsyncCommandExecutor mappingCmd = new QcUnmappingStudyCdmService(dbQueriesService, dbCmdService, netUnitOfWork,
        //         dcmRepository, config);
        //     mappingCmd.RegistrationData(unmappingParameter);
        //     var result = await mappingCmd.Execute();
        //
        //     Assert.AreEqual(true, result);
        // }
    }
}
