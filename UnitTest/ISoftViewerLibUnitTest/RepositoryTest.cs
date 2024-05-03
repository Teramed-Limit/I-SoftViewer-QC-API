using Dicom;
using Dicom.Network;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.Converter;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibUnitTest.FakeData.SCP;
using ISoftViewerLibUnitTest.ToolFuncs;
using ISoftViewerQCSystem.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;
using static ISoftViewerLibrary.Models.DTOs.DICOMConfig.V1;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class RepositoryTest
    {
        [TestMethod]
        public void T1_PatientIods_AreEqual()
        {
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(createStudy.PatientInfo);

            Assert.AreEqual(createStudy.PatientInfo.PatientId, dcmIOD.Patient.PatientId.Value);
            Assert.AreEqual(createStudy.PatientInfo.PatientsName, dcmIOD.Patient.PatientsName.Value);
            Assert.AreEqual(string.Empty, dcmIOD.Patient.OtherPatientId.Value);
        }

        [TestMethod]
        public void T2_StudyIods_AreEqual()
        {
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();

            DicomIODs dcmIOD = new();
            dcmIOD.SetStudy(createStudy.StudyInfo.FirstOrDefault());

            Assert.AreEqual(createStudy.StudyInfo.FirstOrDefault().PatientId, dcmIOD.Studies.First().PatientId.Value);
            Assert.AreEqual(createStudy.StudyInfo.FirstOrDefault().StudyInstanceUID, dcmIOD.Studies.First().StudyInstanceUID.Value);
            Assert.AreEqual(string.Empty, dcmIOD.Studies.First().StudyDate.Value);
        }

        [TestMethod]
        public void T3_SeriesIods_AreEqual()
        {
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();

            DicomIODs dcmIOD = new();
            dcmIOD.SetSeries(createStudy.SeriesInfo.FirstOrDefault());

            Assert.AreEqual(createStudy.SeriesInfo.FirstOrDefault().StudyInstanceUID,
                dcmIOD.Series.First().Value.FirstOrDefault().StudyInstanceUID.Value);

            Assert.AreEqual(createStudy.SeriesInfo.FirstOrDefault().SeriesInstanceUID,
                dcmIOD.Series.First().Value.FirstOrDefault().SeriesInstanceUID.Value);
        }

        [TestMethod]
        public void T4_ImageIods_AreEqual()
        {
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();

            DicomIODs dcmIOD = new();
            dcmIOD.SetImage(createStudy.ImageInfos.First());

            Assert.AreEqual(createStudy.ImageInfos.First().SOPClassUID,
                dcmIOD.Images.First().Value.First().SOPClassUID.Value);
            Assert.AreEqual(createStudy.ImageInfos.First().SOPInstanceUID,
                dcmIOD.Images.First().Value.First().SOPInstanceUID.Value);
            Assert.AreEqual(createStudy.ImageInfos.First().SeriesInstanceUID,
                dcmIOD.Images.First().Value.First().SeriesInstanceUID.Value);
        }

        [TestMethod]
        public void T5_ImageIodsBuffer_IsNotNull()
        {
            string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\DcmSample.dcm";
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
            createStudy.ImageInfos.First().Buffer = System.IO.File.ReadAllBytes(filePath);

            DicomIODs dcmIOD = new();
            dcmIOD.SetImage(createStudy.ImageInfos.First());

            Assert.IsNotNull(dcmIOD.Images.First().Value.First().ImageBuffer);
        }

        [TestMethod]
        public void T6_ConvertDcmBuffer2DicomFile_IsNotNull()
        {

            string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\DcmSample.dcm";
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
            createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);

            DicomIODs dcmIOD = new();
            dcmIOD.SetImage(createStudy.ImageInfos.First());

            IImageBufferConverter<DicomFile> bufferConverter = new DcmBufferConverter();
            DicomFile dcmFile = bufferConverter.Base64BufferToImage(dcmIOD.Images.First().Value.FirstOrDefault().ImageBuffer);

            //dcmFile.Save(@"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\DcmSample1.dcm");
            Assert.IsNotNull(dcmFile);     
        }

        [TestMethod]
        public void T7_ConvertNoneBuffer2DicomFile_IsNotNull()
        {
            string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\Endoscope Capture.bmp";
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
            createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);

            DicomIODs dcmIOD = new();
            dcmIOD.SetImage(createStudy.ImageInfos.First());

            IImageBufferConverter<DicomFile> bufferConverter = new NoneDcmBufferConverter();
            DicomFile dcmFile = bufferConverter.Base64BufferToImage(dcmIOD.Images.First().Value.First().ImageBuffer);
            Assert.IsNotNull(dcmFile);
        }

        [TestMethod]
        public void T8_DcmRepostory_NoneDcm2DicomFile_IsTrue()
        {
            string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\Endoscope Capture.bmp";
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
            createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);
            createStudy.ImageInfos.First().Type = BufferType.btBmp;

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(createStudy.PatientInfo)
                .SetStudy(createStudy.StudyInfo.FirstOrDefault())
                .SetSeries(createStudy.SeriesInfo.FirstOrDefault())
                .SetImage(createStudy.ImageInfos.First());

            IDcmRepository dcmRepository = new DcmOpRepository();
            Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutStore, null);

            Assert.IsTrue(result.Result);
        }

        [TestMethod]
        public void T9_DcmRepostory_NoneDcm2DicomFileConfirmUID_IsTrue()
        {
            string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\Endoscope Capture.bmp";
            CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
            createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);
            createStudy.ImageInfos.First().Type = BufferType.btBmp;

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(createStudy.PatientInfo)
                .SetStudy(createStudy.StudyInfo.FirstOrDefault())
                .SetSeries(createStudy.SeriesInfo.FirstOrDefault())
                .SetImage(createStudy.ImageInfos.First());

            IDcmRepository dcmRepository = new DcmOpRepository();
            Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutStore, null);
            Assert.IsTrue(result.Result);

            DicomDataset dataset = dcmRepository.DicomDatasets.First();
            DicomOperatorHelper dcmHelper = new();

            string patientUID = dcmHelper.GetDicomValueToString(dataset, DicomTag.PatientID, DicomVR.UI, true);
            Assert.AreEqual("NO0001", patientUID);

            string studyUID = dcmHelper.GetDicomValueToString(dataset, DicomTag.StudyInstanceUID, DicomVR.UI, true);
            Assert.AreEqual("1.3.6.1.4.1.57995.1.2.1258350856.19144.1635836821.981", studyUID);

            string seriesUID = dcmHelper.GetDicomValueToString(dataset, DicomTag.SeriesInstanceUID, DicomVR.UI, true);
            Assert.AreEqual("1.3.6.1.4.1.57995.1.3.1258350856.19144.1635836821.982", seriesUID);

            string insUID = dcmHelper.GetDicomValueToString(dataset, DicomTag.SOPInstanceUID, DicomVR.UI, true);
            Assert.AreEqual("1.3.6.1.4.1.57995.1.3.1258350856.19144.1635836821.983", insUID);
        }

        [TestMethod]
        public void TT10_DcmRepostory_UnitOfWork_IsTrue()
        {
            _ = DicomDictionary.Default;
            using (DicomServer.Create<FakeDicomCStoreProvider>(120))
            {
                string filePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\Endoscope Capture.bmp";
                CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
                createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);
                createStudy.ImageInfos.First().Type = BufferType.btBmp;

                DicomIODs dcmIOD = new();
                dcmIOD.SetPatient(createStudy.PatientInfo)
                    .SetStudy(createStudy.StudyInfo.FirstOrDefault())
                    .SetSeries(createStudy.SeriesInfo.FirstOrDefault())
                    .SetImage(createStudy.ImageInfos.First());

                IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                netUnitOfWork.Begin("192.168.1.15", 120, "SCU1", "SCP104", DcmServiceUserType.dsutStore);

                IDcmRepository dcmRepository = new DcmOpRepository();
                netUnitOfWork.RegisterRepository(dcmRepository);

                Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutStore, null);
                Assert.IsTrue(result.Result);
                result = netUnitOfWork.Commit();

                Assert.IsTrue(result.Result);
            }
        }

        [TestMethod]
        public void TT11_DcmRepostory_Query_Patient_UnitOfWork_IsTrue()
        {
            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"));

            using (DicomServer.Create<FakeDicomQRProvider>(120))
            {
                QRServer.AETitle = "QRSCP";
                IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                netUnitOfWork.Begin("192.168.1.15", 120, "SCU1", "QRSCP", DcmServiceUserType.dsutFind);

                IDcmRepository dcmRepository = new DcmOpRepository();
                netUnitOfWork.RegisterRepository(dcmRepository);

                Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutFind, null);
                Assert.IsTrue(result.Result);
                result = netUnitOfWork.Commit();
                Assert.IsTrue(result.Result);
                Assert.IsTrue(!dcmRepository.DicomDatasets.IsEmpty);
            }
        }

        [TestMethod]
        public void TT12_DcmRepostory_Query_ConfirmResult_UnitOfWork_AreEqual()
        {
            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"))
                .SetStudy(new StudyData("1.3.6.1.4.1.54514.20210923103557.1.2718", "PatID_288"));

            using (DicomServer.Create<FakeDicomQRProvider>(120))
            {
                QRServer.AETitle = "QRSCP";
                IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                netUnitOfWork.Begin("192.168.1.15", 120, "SCU1", "QRSCP", DcmServiceUserType.dsutFind);

                IDcmRepository dcmRepository = new DcmOpRepository();
                netUnitOfWork.RegisterRepository(dcmRepository);

                Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutFind, null);
                Assert.IsTrue(result.Result);
                result = netUnitOfWork.Commit();
                Assert.IsTrue(result.Result);
                Assert.IsTrue(!dcmRepository.DicomDatasets.IsEmpty);

                DicomDataset dataste = dcmRepository.DicomDatasets.FirstOrDefault();
                Assert.AreEqual("PatID_288", dataste.GetString(DicomTag.PatientID));
                Assert.AreEqual("PatName_288", dataste.GetString(DicomTag.PatientName));
                Assert.AreEqual("1.3.6.1.4.1.54514.20210923103557.1.2718", dataste.GetString(DicomTag.StudyInstanceUID));
            }
        }

        [TestMethod]
        public void TT13_DcmRepostory_Query_Move_ConfirmResult_UnitOfWork_AreEqual()
        {
            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"))
                .SetStudy(new StudyData("1.3.6.1.4.1.54514.20210923103557.1.2718", "PatID_288"));

            string cstoreFolder = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\CStoreFiles";
            string[] files = Directory.GetFiles(cstoreFolder);
            foreach (var file in files)
            {
                if (File.Exists(file) == true)
                    File.Delete(file);
            }
            _ = DicomDictionary.Default;
            using (DicomServer.Create<FakeDicomCStoreProvider>(104))
            {
                using (DicomServer.Create<FakeDicomQRProvider>(120))
                {
                    QRServer.AETitle = "QRSCP";
                    QRServer.MoveDestinationIP = "192.168.1.15";
                    QRServer.MoveDestinationPort = 104;
                    IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                    
                    var parameters = new Dictionary<string, object>();
                    parameters.Add("moveAE", "STORESCP");
                    netUnitOfWork.Begin("192.168.1.15", 120, "SCU1", "QRSCP", DcmServiceUserType.dsutMove, parameters);

                    IDcmRepository dcmRepository = new DcmOpRepository();
                    netUnitOfWork.RegisterRepository(dcmRepository);

                    Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutMove, null);
                    Assert.IsTrue(result.Result);
                    result = netUnitOfWork.Commit();
                    Assert.IsTrue(result.Result);
                }
            }
            Assert.IsTrue(Directory.GetFiles(cstoreFolder).Length > 0);
        }

        [TestMethod]
        public void TT14_DcmRepostory_Query_Worklist_UnitOfWork_AreEqual()
        {
            StudyData studyData = new();
            studyData.StudyDate = "20211110";
            studyData.Modality = "ES";

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"))
                .SetScheduledProcedureStep(studyData);

            using (DicomServer.Create<FakeDcmWorklistProvider>(104))
            {
                IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                netUnitOfWork.Begin("192.168.1.15", 104, "SCU1", "WLM104", DcmServiceUserType.dsutWorklist);

                IDcmRepository dcmRepository = new DcmOpRepository();
                netUnitOfWork.RegisterRepository(dcmRepository);

                Task<bool> result = dcmRepository.DcmDataEncapsulation(dcmIOD, DcmServiceUserType.dsutWorklist, null);
                Assert.IsTrue(result.Result);
                result = netUnitOfWork.Commit();
                Assert.IsTrue(result.Result);

                Assert.IsTrue(dcmRepository.DicomDatasets.Any());
            }
        }
        [TestMethod]
        public void TT15_Repostory_Service_GetTableName_AreEqual()
        {            
            string name = DbServiceHelper<DICOMProvider>.GetTableName();
            Assert.AreEqual("DicomServiceProvider", name);
            name = DbServiceHelper<DICOMProvider>.GetPrimaryKeyName();
            Assert.AreEqual("Name", name);
        }
    }
}
