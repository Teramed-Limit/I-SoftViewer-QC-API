using Dicom;
using Dicom.Log;
using Dicom.Network;
using Dicom.Network.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ISoftViewerLibUnitTest.FakeData.SCP
{
    #region QRServer
    public static class QRServer
    {
        private static IDicomServer _server;
        public static string AETitle { get; set; }
        public static string MoveDestinationIP { get; set; }
        public static int MoveDestinationPort { get; set; }
        public static IDicomImageFinderService CreateFinderService => new FakeFinderService();

        public static void Start(int port, string aet)
        {
            AETitle = aet;
            _server = DicomServer.Create<FakeDicomQRProvider>(port);
        }
        public static void Stop()
        {
            _server.Dispose();
        }
    }
    #endregion

    #region FakeDicomQRProvider
    public class FakeDicomQRProvider : DicomService, IDicomServiceProvider, IDicomCFindProvider, IDicomCEchoProvider, IDicomCMoveProvider, IDicomCGetProvider
    {
        public FakeDicomQRProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log) 
            : base(stream, fallbackEncoding, log)
        {
            var pi = stream.GetType().GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
            if (pi != null)
            {
                var endPoint = ((Socket)pi.GetValue(stream, null)).RemoteEndPoint as IPEndPoint;
                RemoteIP = endPoint.Address;
            }
            else
            {
                RemoteIP = new IPAddress(new byte[] { 127, 0, 0, 1 });
            }
        }
        #region Fields
        private static readonly DicomTransferSyntax[] AcceptedTransferSyntaxes = new DicomTransferSyntax[]
        {
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };

        private static readonly DicomTransferSyntax[] AcceptedImageTransferSyntaxes = new DicomTransferSyntax[]
        {
            // Lossless
            DicomTransferSyntax.JPEGLSLossless,
            DicomTransferSyntax.JPEG2000Lossless,
            DicomTransferSyntax.JPEGProcess14SV1,
            DicomTransferSyntax.JPEGProcess14,
            DicomTransferSyntax.RLELossless,

            // Lossy
            DicomTransferSyntax.JPEGLSNearLossless,
            DicomTransferSyntax.JPEG2000Lossy,
            DicomTransferSyntax.JPEGProcess1,
            DicomTransferSyntax.JPEGProcess2_4,

            // Uncompressed
            DicomTransferSyntax.ExplicitVRLittleEndian,
            DicomTransferSyntax.ExplicitVRBigEndian,
            DicomTransferSyntax.ImplicitVRLittleEndian
        };

        public string CallingAE { get; protected set; }
        public string CalledAE { get; protected set; }
        public IPAddress RemoteIP { get; private set; }
        #endregion

        #region Methods
        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            Logger.Info($"Received verification request from AE {CallingAE} with IP: {RemoteIP}");
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public void OnConnectionClosed(Exception exception)
        {
            Clean();
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {            
            Logger.Error($"Received abort from {source}, reason is {reason}");
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            Clean();
            return SendAssociationReleaseResponseAsync();
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            CallingAE = association.CallingAE;
            CalledAE = association.CalledAE;

            Logger.Info($"Received association request from AE: {CallingAE} with IP: {RemoteIP} ");

            //if (QRServer.AETitle != CalledAE)
            //{
            //    Logger.Error($"Association with {CallingAE} rejected since called aet {CalledAE} is unknown");
            //    return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            //}

            foreach (var pc in association.PresentationContexts)
            {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFind
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMove
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFind
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMove)
                {
                    pc.AcceptTransferSyntaxes(AcceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelGet
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelGet)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None)
                {
                    pc.AcceptTransferSyntaxes(AcceptedImageTransferSyntaxes);
                }
                else
                {
                    Logger.Warn($"Requested abstract syntax {pc.AbstractSyntax} from {CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Logger.Info($"Accepted association request from {CallingAE}");
            return SendAssociationAcceptAsync(association);
        }

        public IEnumerable<DicomCFindResponse> OnCFindRequest(DicomCFindRequest request)
        {
            var queryLevel = request.Level;

            var matchingFiles = new List<string>();
            IDicomImageFinderService finderService = QRServer.CreateFinderService;            

            switch (queryLevel)
            {
                case DicomQueryRetrieveLevel.Patient:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);

                        matchingFiles = finderService.FindPatientFiles(patname, patid);
                    }
                    break;

                case DicomQueryRetrieveLevel.Study:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);

                        matchingFiles = finderService.FindStudyFiles(patname, patid, accNr, studyUID);
                    }
                    break;

                case DicomQueryRetrieveLevel.Series:
                    {
                        var patname = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty);
                        var patid = request.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty);
                        var accNr = request.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty);
                        var studyUID = request.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty);
                        var seriesUID = request.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty);
                        var modality = request.Dataset.GetSingleValueOrDefault(DicomTag.Modality, string.Empty);

                        matchingFiles = finderService.FindSeriesFiles(patname, patid, accNr, studyUID, seriesUID, modality);
                    }
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCFindResponse(request, DicomStatus.QueryRetrieveUnableToProcess);
                    yield break;
            }
            
            foreach (var matchingFile in matchingFiles)
            {
                var dicomFile = DicomFile.Open(matchingFile);
                var result = new DicomDataset();
                foreach (var requestedTag in request.Dataset)
                {                    
                    if (dicomFile.Dataset.Contains(requestedTag.Tag))
                    {
                        dicomFile.Dataset.CopyTo(result, requestedTag.Tag);
                    }
                    // else if (requestedTag == DicomTag.NumberOfStudyRelatedInstances)
                    // {                    
                    //    result.Add(DicomTag.NumberOfStudyRelatedInstances, number);
                    // } ....
                    else
                    {
                        result.Add(requestedTag);
                    }
                }
                yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
            }

            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }

        public void Clean()
        {
            // cleanup, like cancel outstanding move- or get-jobs
        }

        public IEnumerable<DicomCMoveResponse> OnCMoveRequest(DicomCMoveRequest request)
        {            
            if (request.DestinationAE != "STORESCP")
            {
                yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveMoveDestinationUnknown);
                yield return new DicomCMoveResponse(request, DicomStatus.ProcessingFailure);
                yield break;
            }
            
            var destinationPort = QRServer.MoveDestinationPort;
            var destinationIP = QRServer.MoveDestinationIP;

            IDicomImageFinderService finderService = QRServer.CreateFinderService;
            IEnumerable<string> matchingFiles = Enumerable.Empty<string>();

            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetSingleValue<string>(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveUnableToPerformSuboperations);
                    yield break;
            }

            var client = new Dicom.Network.Client.DicomClient(destinationIP, destinationPort, false, QRServer.AETitle, request.DestinationAE);
            client.NegotiateAsyncOps();
            int storeTotal = matchingFiles.Count();
            int storeDone = 0; 
            int storeFailure = 0; 
            foreach (string file in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(file);                
                storeRequest.OnResponseReceived += (req, resp) =>
                {
                    if (resp.Status == DicomStatus.Success)
                    {
                        Logger.Info("Storage of image successfull");
                        storeDone++;
                    }
                    else
                    {
                        Logger.Error("Storage of image failed");
                        storeFailure++;
                    }
                };
                client.AddRequestAsync(storeRequest).Wait();
            }

            var sendTask = client.SendAsync();

            while (!sendTask.IsCompleted)
            {                
                yield return new DicomCMoveResponse(request, DicomStatus.Pending) { Remaining = storeTotal - storeDone - storeFailure, Completed = storeDone };
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            Logger.Info("..finished");
            yield return new DicomCMoveResponse(request, DicomStatus.Success);
        }


        public IEnumerable<DicomCGetResponse> OnCGetRequest(DicomCGetRequest request)
        {
            IDicomImageFinderService finderService = QRServer.CreateFinderService;
            IEnumerable<string> matchingFiles = Enumerable.Empty<string>();

            switch (request.Level)
            {
                case DicomQueryRetrieveLevel.Patient:
                    matchingFiles = finderService.FindFilesByUID(request.Dataset.GetSingleValue<string>(DicomTag.PatientID), string.Empty, string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Study:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID), string.Empty);
                    break;

                case DicomQueryRetrieveLevel.Series:
                    matchingFiles = finderService.FindFilesByUID(string.Empty, string.Empty, request.Dataset.GetSingleValue<string>(DicomTag.SeriesInstanceUID));
                    break;

                case DicomQueryRetrieveLevel.Image:
                    yield return new DicomCGetResponse(request, DicomStatus.QueryRetrieveUnableToPerformSuboperations);
                    yield break;
            }

            foreach (var matchingFile in matchingFiles)
            {
                var storeRequest = new DicomCStoreRequest(matchingFile);
                SendRequestAsync(storeRequest).Wait();
            }

            yield return new DicomCGetResponse(request, DicomStatus.Success);
        }

        #endregion
    }
    #endregion

    #region IDicomImageFinderService
    public interface IDicomImageFinderService
    {

        /// <summary>
        /// Searches in a DICOM store for patient information. Returns a representative DICOM file per found patient
        /// </summary>
        List<string> FindPatientFiles(string PatientName, string PatientId);

        /// <summary>
        /// Searches in a DICOM store for study information. Returns a representative DICOM file per found study
        /// </summary>
        List<string> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID);

        /// <summary>
        /// Searches in a DICOM store for series information. Returns a representative DICOM file per found serie
        /// </summary>
        List<string> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality);

        /// <summary>
        /// Searches in a DICOM store for all files matching the given UIDs
        /// </summary>
        List<string> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID);

        string _storagePath { get; set; }

    }
    #endregion

    #region StupidSlowFinderService
    public class FakeFinderService : IDicomImageFinderService
    {
        public string _storagePath { get; set; } = @"D:\WorkSpace\Victor\22.QCWorkstation\Sources\SampleDatas";        

        public List<string> FindPatientFiles(string PatientName, string PatientId) =>            
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    return matches;
                });


        public List<string> FindStudyFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID) =>            
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    return matches;
                });


        public List<string> FindSeriesFiles(string PatientName, string PatientId, string AccessionNbr, string StudyUID, string SeriesUID, string Modality) =>            
            SearchInFilesystem(
                dcmFile => dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty),
                dcmFile =>
                {
                    bool matches = true;
                    matches &= MatchFilter(PatientName, dcmFile.GetSingleValueOrDefault(DicomTag.PatientName, string.Empty));
                    matches &= MatchFilter(PatientId, dcmFile.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(AccessionNbr, dcmFile.GetSingleValueOrDefault(DicomTag.AccessionNumber, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));
                    matches &= MatchFilter(Modality, dcmFile.GetSingleValueOrDefault(DicomTag.Modality, string.Empty));
                    return matches;
                });


        private List<string> SearchInFilesystem(Func<DicomDataset, string> level, Func<DicomDataset, bool> matches)
        {
            string dicomRootDirectory = _storagePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>(); 
            var foundKeys = new List<string>(); 

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    var key = level(dcmFile.Dataset);
                    if (!string.IsNullOrEmpty(key)
                        && !foundKeys.Contains(key)
                        && matches(dcmFile.Dataset))
                    {
                        matchingFiles.Add(fileNameToTest);
                        foundKeys.Add(key);
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            return matchingFiles;
        }

        public List<string> FindFilesByUID(string PatientId, string StudyUID, string SeriesUID)
        {            
            string dicomRootDirectory = _storagePath;
            var allFilesOnHarddisk = Directory.GetFiles(dicomRootDirectory, "*.dcm", SearchOption.AllDirectories);
            var matchingFiles = new List<string>();

            foreach (string fileNameToTest in allFilesOnHarddisk)
            {
                try
                {
                    var dcmFile = DicomFile.Open(fileNameToTest);

                    bool matches = true;
                    matches &= MatchFilter(PatientId, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, string.Empty));
                    matches &= MatchFilter(StudyUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyInstanceUID, string.Empty));
                    matches &= MatchFilter(SeriesUID, dcmFile.Dataset.GetSingleValueOrDefault(DicomTag.SeriesInstanceUID, string.Empty));

                    if (matches)
                    {
                        matchingFiles.Add(fileNameToTest);
                    }
                }
                catch (Exception)
                {
                    
                }
            }
            return matchingFiles;
        }

        private bool MatchFilter(string filterValue, string valueToTest)
        {
            if (string.IsNullOrEmpty(filterValue))
            {                
                return true;
            }            
            var filterRegex = "^" + Regex.Escape(filterValue).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(valueToTest, filterRegex, RegexOptions.IgnoreCase);
        }
    }
    #endregion
}
