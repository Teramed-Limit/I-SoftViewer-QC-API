using Dicom;
using Dicom.Log;
using Dicom.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ISoftViewerLibUnitTest.FakeData.SCP
{
    public class FakeDcmWorklistProvider : DicomService, IDicomServiceProvider, IDicomCFindProvider
    {
        private static DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };

        public FakeDcmWorklistProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log)
            : base(stream, fallbackEncoding, log)
        {
        }        

        public void OnConnectionClosed(Exception exception)
        {

        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {

        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            Logger.Info($"Received association request from AE: {association.CallingAE} with IP: {association.RemoteHost} ");            

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification || pc.AbstractSyntax == DicomUID.ModalityWorklistInformationModelFind)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else
                {
                    Logger.Warn($"Requested abstract syntax {pc.AbstractSyntax} from {association.CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.Info($"Accepted association request from {association.CallingAE}");
            return SendAssociationAcceptAsync(association);
        }        

        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }

        protected int RandomValue;

        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            //處理狀態
            DicomStatus status = DicomStatus.Success;
            //回覆Client的查詢結果
            List<DicomCFindResponse> responses = new List<DicomCFindResponse>();

            //先判斷有無查詢條件
            if (request.HasDataset == false)
            {
                //沒條件不處理,直接回覆失敗
                responses.Add(new DicomCFindResponse(request, DicomStatus.InvalidAttributeValue));
                return responses;
            }

            Random rnd = new Random();
            //第一個病人檢查
            RandomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
            DicomCFindResponse rsp1 = new(request, DicomStatus.Pending)
            { Dataset = CreateResponseDataset("EBUS") };
            responses.Add(rsp1);

            RandomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
            DicomCFindResponse rsp2 = new(request, DicomStatus.Pending)
            { Dataset = CreateResponseDataset("Cholangiogram") };
            responses.Add(rsp2);
            //第二個病人檢查
            //RandomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
            //DicomCFindResponse rsp3 = new(request, DicomStatus.Pending)
            //{ Dataset = CreateResponseDataset("Colonosopy") };
            //responses.Add(rsp3);

            //RandomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
            //DicomCFindResponse rsp4 = new(request, DicomStatus.Pending)
            //{ Dataset = CreateResponseDataset("Duodenoscopy") };
            //responses.Add(rsp4);

            responses.Add(new DicomCFindResponse(request, status));
            return responses;
        }

        protected DicomDataset CreateResponseDataset(string examType)
        {
            TimeSpan timeSpan = DateTime.Now.Subtract(new DateTime(2000, 05, 01, 10, 28, 50));
            DateTime birthday = DateTime.Now.Subtract(timeSpan);

            DicomDataset rspDataset = new()
            {
                { DicomTag.PatientID, "PatID_" + Convert.ToString(RandomValue) },
                { DicomTag.PatientName, "PatName_" + Convert.ToString(RandomValue) },
                { DicomTag.IssuerOfPatientID, "IssuerOfPatientID" },
                { DicomTag.PatientSex, "M" },
                { DicomTag.PatientWeight, "50" },
                { DicomTag.PatientBirthDate, birthday.ToString("yyyyMMdd") },
                { DicomTag.MedicalAlerts, "MedicalAlerts" },
                { DicomTag.PregnancyStatus, new ushort[0] },
                { DicomTag.Allergies, "Allergies" },
                { DicomTag.PatientComments, "PatientComments" },
                { DicomTag.SpecialNeeds, "SpecialNeeds" },
                { DicomTag.PatientState, "PatientState" },
                { DicomTag.CurrentPatientLocation, "CurrentPatientLocation" },
                { DicomTag.InstitutionName, "UnitTest" },
                { DicomTag.AdmissionID, "AdmissionID" },
                { DicomTag.AccessionNumber, "AccNum_" + Convert.ToString(RandomValue) },
                { DicomTag.ReferringPhysicianName, "Unit Test Doctor" },
                { DicomTag.AdmittingDiagnosesDescription, "AdmittingDiagnosesDescription" },
                { DicomTag.RequestingPhysician, "Unit Test Requesting Doctor" },
                { DicomTag.StudyInstanceUID, GenerateNewStudyInstanceUID() },
                { DicomTag.StudyDescription, examType },
                { DicomTag.StudyID, "UnitTestST001" },
                { DicomTag.ReasonForTheRequestedProcedure, "ReasonForTheRequestedProcedure" },
                { DicomTag.StudyDate, DateTime.Now.ToString("yyyyMMdd") },
                { DicomTag.StudyTime, DateTime.Now.ToString("hhmmss") },

                { DicomTag.RequestedProcedureID, "ReqID_" + Convert.ToString(RandomValue) },
                { DicomTag.RequestedProcedureDescription, examType },
                { DicomTag.RequestedProcedurePriority, string.Empty },
                new DicomSequence(DicomTag.RequestedProcedureCodeSequence),
                new DicomSequence(DicomTag.ReferencedStudySequence),

                new DicomSequence(DicomTag.ProcedureCodeSequence)
            };

            var sps = new DicomDataset
            {
                { DicomTag.ScheduledStationAETitle, "Unit Test" },
                { DicomTag.ScheduledStationName, "Unit Test Name" },
                { DicomTag.ScheduledProcedureStepStartDate, DateTime.Now.ToString("yyyyMMdd") },
                { DicomTag.ScheduledProcedureStepStartTime, DateTime.Now.ToString("hhmmss") },
                { DicomTag.Modality, "ES" },
                { DicomTag.ScheduledPerformingPhysicianName, "Scheduled Doctor Name" },
                { DicomTag.ScheduledProcedureStepDescription, "Scheduled Description" },
                new DicomSequence(DicomTag.ScheduledProtocolCodeSequence),
                { DicomTag.ScheduledProcedureStepLocation, "Location" },
                { DicomTag.ScheduledProcedureStepID, "ScheduledID_" + Convert.ToString(RandomValue) },
                { DicomTag.RequestedContrastAgent, "RequestedContrastAgent" },
                { DicomTag.PreMedication, "PreMedication" },
                { DicomTag.AnatomicalOrientationType, string.Empty }
            };
            rspDataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps));

            return rspDataset;
        }

        protected string GenerateNewStudyInstanceUID()
        {
            Thread.Sleep(100);
            string timespan = DateTime.Now.ToString("fff");
            Random rnd = new Random();
            int randomValue = Enumerable.Range(1, 9999).OrderBy(x => rnd.Next()).Take(1000).ToList().First();
            return "1.3.6.1.4.1.54514." + DateTime.Now.ToString("yyyyMMddhhmmss") + "." +
                                "1." + Convert.ToString(randomValue) + "." + timespan;
        }        
    }
}
