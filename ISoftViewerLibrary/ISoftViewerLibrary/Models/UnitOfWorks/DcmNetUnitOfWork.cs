using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Dicom;
using Dicom.Network;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Utils;
using static ISoftViewerLibrary.Models.ValueObjects.Types;
using DicomClient = Dicom.Network.Client.DicomClient;

namespace ISoftViewerLibrary.Models.UnitOfWorks
{
    /// <summary>
    /// DICOM C-Service作業
    /// </summary>
    public class DcmNetUnitOfWork : IDcmUnitOfWork, IOpMessage
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="moveDestination"></param>
        public DcmNetUnitOfWork()
        {
            MoveDestination = "";
            DicomRepository = null;
            Message = "";
            Result = OpResult.OpSuccess;

            ServiceFunc = new Dictionary<DcmServiceUserType, Func<Task<bool>>>
            {
                { DcmServiceUserType.dsutStore, CStoreService },
                { DcmServiceUserType.dsutFind, CFindService },
                { DcmServiceUserType.dsutMove, CMoveService },
                { DcmServiceUserType.dsutWorklist, CFindWorklistService },
                { DcmServiceUserType.dsutEcho, CEchoService }
            };

            ServiceMessages = new List<string>();
        }

        #region Fields

        /// <summary>
        /// DICOM處理庫
        /// </summary>
        protected IDcmRepository DicomRepository;

        /// <summary>
        /// 
        /// </summary>
        protected DicomClient DcmClient;

        /// <summary>
        /// 要處理的DICOM檔案
        /// </summary>
        protected ConcurrentBag<DicomDataset> DcmDatasets;

        /// <summary>
        /// DICOM服務類型
        /// </summary>
        protected DcmServiceUserType ServiceType;

        /// <summary>
        /// 服務容器
        /// </summary>
        protected Dictionary<DcmServiceUserType, Func<Task<bool>>> ServiceFunc;

        /// <summary>
        /// 服務訊息列表
        /// </summary>
        protected List<string> ServiceMessages;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 處理結果
        /// </summary>
        public OpResult Result { get; private set; }

        /// <summary>
        /// 移動目的地
        /// </summary>
        private string MoveDestination;

        #region Methods

        /// <summary>
        /// 註冊處理庫
        /// </summary>
        /// <param name="dcmRepository"></param>
        public void RegisterRepository(IDcmCqusDatasets dcmCqusDatasets)
        {
            DcmDatasets = dcmCqusDatasets.DicomDatasets;
        }

        /// <summary>
        /// 開始DICOM網路服務
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        /// <returns></returns>
        public bool Begin(string host, int port, string callingAe, string calledAe, DcmServiceUserType type,
            Dictionary<string, object> parameter)
        {
            Message = "";
            Result = OpResult.OpSuccess;
            ServiceType = type;
            MoveDestination = "";
            if (parameter != null && parameter.TryGetValue("moveAE", out var value))
                MoveDestination = value.ToString();

            DcmClient = new DicomClient(host, port, false, callingAe, calledAe);
            DcmClient.AssociationRejected += (sender, args) =>
            {
                Serilog.Log.Debug("=====================Dicom Service Association Rejected=====================");
                Serilog.Log.Debug("Reject Reason: {ArgsReason}", args.Reason);
                Serilog.Log.Debug("Reject Result: {ArgsResult}", args.Result);
                Serilog.Log.Debug("Reject Source: {ArgsSource}", args.Source);
            };

            DcmClient.AssociationAccepted += (sender, args) =>
            {
                Serilog.Log.Debug("=====================Dicom Service Association Accepted=====================");
                Serilog.Log.Debug("Remote Host: {AssociationRemoteHost}", args.Association.RemoteHost);
                Serilog.Log.Debug("Remote Port: {AssociationRemotePort}", args.Association.RemotePort);
                Serilog.Log.Debug("CallingAe: {AssociationCallingAe}", args.Association.CallingAE);
                Serilog.Log.Debug("CalledAe: {AssociationCalledAe}", args.Association.CalledAE);
                Serilog.Log.Debug("MoveAE: {MoveAe}", MoveDestination);
                Serilog.Log.Debug("RemoteImplementationClassUID: {AssociationRemoteImplementationClassUid}",
                    args.Association.RemoteImplementationClassUID);
                foreach (var presentationContext in args.Association.PresentationContexts)
                {
                    Serilog.Log.Debug("AbstractSyntax: {PresentationContextAbstractSyntax}",
                        presentationContext.AbstractSyntax);
                    Serilog.Log.Debug("AcceptedTransferSyntax: {PresentationContextAcceptedTransferSyntax}",
                        presentationContext.AcceptedTransferSyntax);
                }
            };

            DcmClient.AssociationReleased += (sender, args) =>
            {
                Serilog.Log.Debug("=====================Dicom Service Association Released=====================");
            };


            DcmClient.NegotiateAsyncOps();
            //未來這裡可以設定是否要先做ECHO的動作

            return true;
        }

        /// <summary>
        /// 確定處理
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Commit()
        {
            try
            {
                //避免HOST和IP錯誤而造成系統卡住
                using (Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    if (socket.BeginConnect(IPAddress.Parse(DcmClient.Host), DcmClient.Port, null, null)
                            .AsyncWaitHandle.WaitOne(2000, true) == false)
                        throw new Exception("Peer service IP and Port can not used");
                }

                var enumerable = ServiceFunc.Where(x => x.Key == ServiceType);
                if (enumerable.Any() == false)
                    throw new Exception("Illegal DICOM service");

                Func<Task<bool>> func = enumerable.First().Value;
                if (func() == Task.FromResult(false))
                    throw new Exception(Message);

                //傳送服務
                await DcmClient.SendAsync();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return await Task.FromResult(false);
            }

            return await Task.FromResult(true);
        }

        /// <summary>
        /// 取消復原
        /// </summary>
        public Task<bool> Rollback()
        {
            DcmClient = null;
            return Task.FromResult(true);
        }

        /// <summary>
        /// C-Store
        /// </summary>
        protected async Task<bool> CStoreService()
        {
            ServiceMessages.Clear();
            try
            {
                foreach (var dataset in DcmDatasets)
                {
                    DicomFile dcff = new(dataset);
                    var request = new DicomCStoreRequest(dcff);
                    request.OnResponseReceived += (req, rsp) =>
                    {
                        ServiceMessages.Add("C-Store Response Received, Status: " + rsp.Status);
                    };
                    Serilog.Log.Debug($"*** Request Dataset ***");
                    Serilog.Log.Debug($"SOPClassUID: {request.SOPClassUID}");
                    Serilog.Log.Debug($"SOPInstanceUID: {request.SOPInstanceUID}");
                    Serilog.Log.Debug($"TransferSyntax: {request.TransferSyntax}");
                    await DcmClient.AddRequestAsync(request);
                }

                Serilog.Log.Debug($"Number of dataset: {DcmDatasets.Count}");
                // DcmDatasets.Clear();
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return true;
        }

        /// <summary>
        /// C-Find
        /// </summary>
        protected async Task<bool> CFindService()
        {
            ServiceMessages.Clear();
            try
            {
                if (DcmDatasets.IsEmpty == true)
                    throw new Exception("QR condition dataset cannot be empty");

                DicomDataset dataset = DcmDatasets.First();
                var request = new DicomCFindRequest(GetQueryRetrieveLevel(dataset))
                {
                    Dataset = new DicomDataset(dataset)
                };

                Serilog.Log.Debug("===============Request Dataset===============");
                await new DicomDatasetWalker(request.Dataset).WalkAsync(new DatasetWalker());
                Serilog.Log.Debug("=============================================");
                request.OnResponseReceived += (req, response) =>
                {
                    if (response.Dataset == null) return;
                    Serilog.Log.Debug("=============================================");
                    new DicomDatasetWalker(response.Dataset).Walk(new DatasetWalker());
                    DcmDatasets.Add(response.Dataset);
                };
                DcmDatasets.Clear();

                await DcmClient.AddRequestAsync(request);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return true;
        }

        /// <summary>
        /// Worklist Service 
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CFindWorklistService()
        {
            ServiceMessages.Clear();
            try
            {
                if (DcmDatasets.IsEmpty == true)
                    throw new Exception("QR condition dataset cannot be empty");

                DicomDataset dataset = DcmDatasets.First();
                DicomCFindRequest cfindRq = DicomCFindRequest.CreateWorklistQuery();
                cfindRq.Dataset = new DicomDataset(dataset);

                Serilog.Log.Debug("=====================Request Dataset=====================");
                await new DicomDatasetWalker(cfindRq.Dataset).WalkAsync(new DatasetWalker());
                cfindRq.OnResponseReceived = (DicomCFindRequest rq, DicomCFindResponse rp) =>
                {
                    if (rp.HasDataset)
                    {
                        ServiceMessages.Add(
                            $"Study UID: {rp.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID)}");
                        Serilog.Log.Debug("=============================================");
                        new DicomDatasetWalker(rp.Dataset).Walk(new DatasetWalker());
                        DcmDatasets.Add(rp.Dataset);
                    }
                    else
                    {
                        ServiceMessages.Add(rp.Status.ToString());
                    }
                };
                DcmDatasets.Clear();

                await DcmClient.AddRequestAsync(cfindRq);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return true;
        }

        protected async Task<bool> CEchoService()
        {
            ServiceMessages.Clear();
            try
            {
                var cEchoRequest = new DicomCEchoRequest
                {
                    OnResponseReceived = (DicomCEchoRequest rq, DicomCEchoResponse rp) =>
                    {
                        ServiceMessages.Add(rp.Status.ToString());
                    }
                };

                await DcmClient.AddRequestAsync(cEchoRequest);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return true;
        }

        /// <summary>
        /// C-MOVE
        /// </summary>
        /// <returns></returns>
        protected async Task<bool> CMoveService()
        {
            ServiceMessages.Clear();
            bool moveSuccessfully = false;
            try
            {
                if (DcmDatasets.IsEmpty == true)
                    throw new Exception("QR condition dataset cannot be empty");

                DicomDataset dataset = DcmDatasets.First();
                //取得最基本的Study Instance UID資料
                if (dataset.TryGetString(DicomTag.StudyInstanceUID, out string stUID) == false)
                    throw new Exception("Not found Study Instance UID in dataset");
                //目前只支援Study & Series Level
                DicomCMoveRequest cMoveRequest = null;
                switch (GetQueryRetrieveLevel(dataset))
                {
                    case DicomQueryRetrieveLevel.Study:
                        cMoveRequest = new DicomCMoveRequest(MoveDestination, stUID);
                        break;
                    case DicomQueryRetrieveLevel.Series:
                        if (dataset.TryGetString(DicomTag.SeriesInstanceUID, out string seUID) == false)
                            throw new Exception("Not found Series Instance UID in dataset");

                        cMoveRequest = new DicomCMoveRequest(MoveDestination, stUID, seUID);
                        break;
                }

                Serilog.Log.Debug($"*** Request Dataset ***");
                await new DicomDatasetWalker(cMoveRequest.Dataset).WalkAsync(new DatasetWalker());
                cMoveRequest.OnResponseReceived += (DicomCMoveRequest request, DicomCMoveResponse response) =>
                {
                    if (response.Status.State == DicomState.Pending)
                    {
                        ServiceMessages.Add("Sending is in progress. please wait: " + response.Remaining.ToString());
                    }
                    else if (response.Status.State == DicomState.Success)
                    {
                        ServiceMessages.Add("Sending successfully finished");
                        moveSuccessfully = true;
                    }
                    else if (response.Status.State == DicomState.Failure)
                    {
                        ServiceMessages.Add("Error sending datasets: " + response.Status.Description);
                        moveSuccessfully = false;
                    }

                    ServiceMessages.Add(response.Status.ToString());
                };
                DcmDatasets.Clear();
                await DcmClient.AddRequestAsync(cMoveRequest);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return moveSuccessfully;
        }

        /// <summary>
        /// 取得FIND和MOVE的層別
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        protected DicomQueryRetrieveLevel GetQueryRetrieveLevel(DicomDataset dataset)
        {
            DicomQueryRetrieveLevel result = DicomQueryRetrieveLevel.NotApplicable;
            try
            {
                if (dataset.TryGetString(DicomTag.QueryRetrieveLevel, out string level) == false ||
                    (level.Trim() != "STUDY" && level.Trim() != "SERIES"))
                    throw new Exception($"Lost the tag{DicomTag.QueryRetrieveLevel}");

                result = (level.Trim() == "STUDY") ? DicomQueryRetrieveLevel.Study : DicomQueryRetrieveLevel.Series;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return result;
        }

        #endregion
    }

    #endregion
}