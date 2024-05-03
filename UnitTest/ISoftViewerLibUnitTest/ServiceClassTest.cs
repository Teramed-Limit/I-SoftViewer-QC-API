using Dicom.Network;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using ISoftViewerLibUnitTest.FakeData.SCP;
using ISoftViewerLibUnitTest.ToolFuncs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibUnitTest
{
    [TestClass]
    public class ServiceClassTest
    {
        [TestMethod]
        public async Task T1_DcmQueries_Worklist_Count_AreEqual()
        {
            IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
            IDcmRepository dcmRepository = new DcmOpRepository();
            EnvironmentConfiguration config = new();

            IDcmQueries dcmQryService = new DcmQueriesService(netUnitOfWork, dcmRepository, null, config);

            StudyData studyData = new();
            studyData.StudyDate = "20211110";
            studyData.Modality = "ES";

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"))
                .SetScheduledProcedureStep(studyData);

            Queries.V1.QueryResult result;
            using (DicomServer.Create<FakeDcmWorklistProvider>(130))
            {
                result = await dcmQryService.FindDataJson(dcmIOD, DcmServiceUserType.dsutWorklist, "192.168.1.15", 130, "SCU1",
                    "WLM104", null);

                Thread.Sleep(1000);
            }
            Assert.IsTrue(result.Datasets.Any());
            Assert.AreEqual(result.Datasets.Count, 2);
        }
        [TestMethod]
        public async Task T2_DcmQueries_Query_Count_AreEqual()
        {
            IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
            IDcmRepository dcmRepository = new DcmOpRepository();
            EnvironmentConfiguration config = new();

            IDcmQueries dcmQryService = new DcmQueriesService(netUnitOfWork, dcmRepository, null, config);

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("PatID_288", "PatName_288"))
                .SetStudy(new StudyData("1.3.6.1.4.1.54514.20210923103557.1.2718", "PatID_288"));

            Queries.V1.QueryResult result;
            using (DicomServer.Create<FakeDicomQRProvider>(131))
            {
                QRServer.AETitle = "QRSCP";
                result = await dcmQryService.FindDataJson(dcmIOD, DcmServiceUserType.dsutFind, "192.168.1.15",
                    131, "SCU1", "QRSCP", null);

                Thread.Sleep(1000);
            }
            Assert.IsTrue(result.Datasets.Any());
            Assert.AreEqual(result.Datasets.Count, 1);
        }
        [TestMethod]
        public async Task T3_DcmQueries_Move_Count_ArNotEqual()
        {
            IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
            IDcmRepository dcmRepository = new DcmOpRepository();
            EnvironmentConfiguration config = new()
            {
                VirtualFilePath = "http://127.0.0.1/UnitTest/",
                ServerName = "tcp:localhost",
                DatabaseName = "PACSServer",
                DBUserID = "sa",
                DBPassword = "victor70394219"
            };

            DbQueriesService<CustomizeTable> dbQueriesService = new(
                config.DBUserID, config.DBPassword, config.DatabaseName, config.ServerName);

            IDcmQueries dcmQryService = new DcmQueriesService(netUnitOfWork, dcmRepository, dbQueriesService, config);

            DicomIODs dcmIOD = new();
            dcmIOD.SetPatient(new PatientData("UY456789(4)", "HOO Kin Hong"))
                .SetStudy(new StudyData("1.2.840.113564.345050855863.11952.637677404597546369.66", "UY456789(4)"));

            string cstoreFolder = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\CStoreFiles";
            string[] files = Directory.GetFiles(cstoreFolder);
            foreach (var file in files)
            {
                if (File.Exists(file) == true)
                    File.Delete(file);
            }

            Queries.V1.QueryResult result;
            using (DicomServer.Create<FakeDicomCStoreProvider>(132))
            {
                using (DicomServer.Create<FakeDicomQRProvider>(133))
                {
                    QRServer.AETitle = "QRSCP";
                    QRServer.MoveDestinationIP = "192.168.1.15";
                    QRServer.MoveDestinationPort = 133;
                    QRServer.CreateFinderService._storagePath = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas\CMoveFiles";

                    var parameters = new Dictionary<string, object>();
                    parameters.Add("moveAE", "STORESCP");
                    result = await dcmQryService.FindDataJson(dcmIOD, DcmServiceUserType.dsutMove, "192.168.1.15",
                                132, "SCU1", "QRSCP", parameters);

                    Thread.Sleep(500);
                }
            }

            Assert.IsTrue(result.FileSetIDs.Any());
            Assert.AreNotEqual(result.FileSetIDs, 0);
        }

        [TestMethod]
        public async Task T4_DcmCommand_Store_IsTrue()
        {
            using (DicomServer.Create<FakeDicomCStoreProvider>(134))
            {
                string filePath = @"C:\Users\Romeo\Desktop\CeZu1YjUsAEfhcP.jpg";
                CreateAndModifyStudy<ImageBufferAndData> createStudy = ToolFunc.GetDicomIODs();
                createStudy.ImageInfos.First().Buffer = File.ReadAllBytes(filePath);
                createStudy.ImageInfos.First().Type = BufferType.btBmp;

                DicomIODs dcmIOD = new();
                dcmIOD.SetPatient(createStudy.PatientInfo)
                    .SetStudy(createStudy.StudyInfo.FirstOrDefault())
                    .SetSeries(createStudy.SeriesInfo.FirstOrDefault())
                    .SetImage(createStudy.ImageInfos.First());

                IDcmUnitOfWork netUnitOfWork = new DcmNetUnitOfWork();
                IDcmRepository dcmRepository = new DcmOpRepository();

                IDcmCommand dcmCmdService = new DcmCommandService(netUnitOfWork, dcmRepository);
                DicomOperationNodes node = new()
                {
                    Name = "SCP104",
                    OperationType = "C-STORE",
                    AETitle = "SCU1",
                    RemoteAETitle = "SCP104",
                    IPAddress = "192.168.1.2",
                    Port = 104
                };
                var result = await dcmCmdService.Update(dcmIOD, DcmServiceUserType.dsutStore, new () { node });

                Assert.IsTrue(result);
            }
        }
    }
}
