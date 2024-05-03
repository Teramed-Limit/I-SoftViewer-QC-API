using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;

namespace ISoftViewerQCSystem.Applications
{
    /// <summary>
    ///     DICOM檢查命令應用層服務(Create,Update,Delete)
    /// </summary>
    public class DcmDataCmdApplicationService : IApplicationCmdService
    {
        /// <summary>
        ///     建構
        /// </summary>
        public DcmDataCmdApplicationService(
            IDcmCommand dcmCmdService,
            DicomOperationNodeService dicomOperationNodeService,
            QCOperationContext qcOperationContext)
        {
            DicomCmdService = dcmCmdService;
            QCOperationContext = qcOperationContext;
            _dicomOperationNodeService = dicomOperationNodeService;
        }

        #region Methods

        /// <summary>
        ///     處理命令
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task<Queries.V1.CommandResult> Handle(string userName, object command)
        {
            Queries.V1.CommandResult cmdResult = null;
            try
            {
                switch (command)
                {
                    case DataCorrection.V1.CreateAndModifyStudy<DataCorrection.V1.ImageBufferAndData> cmd:
                        Serilog.Log.Information(
                            "Handle the CreateAndModifyStudy service(ImageBufferAndData) start ======================");

                        DicomIODs dcmIOD = new();
                        dcmIOD.SetPatient(cmd.PatientInfo);
                        cmd.StudyInfo.ForEach(study => dcmIOD.SetStudy(study));
                        cmd.SeriesInfo.ForEach(series => dcmIOD.SetSeries(series));
                        cmd.ImageInfos.ForEach(img => dcmIOD.SetImage(img));
                        Task<bool> tmpResult;

                        // 最後一個永遠是設定檔的PACS Server
                        var nodesList = cmd.SendOtherEnableNodes
                            ? _dicomOperationNodeService.GetAllCStoreNode()
                            : new List<DicomOperationNodes> { _dicomOperationNodeService.GetTeramedCStoreNode() };

                        tmpResult = DicomCmdService.Update(dcmIOD, Types.DcmServiceUserType.dsutStore, nodesList);

                        if (tmpResult.Result == false)
                            throw new Exception(DicomCmdService.Message);

                        cmdResult = new Queries.V1.CommandResult();
                        cmdResult.Resultes.Add(new DataCorrection.V1.DcmTagData
                            { Group = 0, Elem = 0, Value = bool.TrueString });

                        // TODO: 這不是即時，可以放到背景做
                        // 記錄使用者操作
                        // 最後一個是自己的PACS Server，先記錄
                        nodesList.Reverse();
                        foreach (var studyData in cmd.StudyInfo)
                        foreach (var node in nodesList)
                            // 指派使用者操作
                            if (cmd.SendOtherEnableNodes)
                            {
                                if (_dicomOperationNodeService.IsTeramedCStoreNode(node))
                                {
                                    QCOperationContext.SetLogger(new ImportStudyLogger());
                                    QCOperationContext.SetParams(userName, studyData.StudyInstanceUID, "", null);
                                    QCOperationContext.WriteSuccessRecord();
                                    continue;
                                }

                                QCOperationContext.SetLogger(new SendToPacsLogger());
                                QCOperationContext.SetParams(userName, studyData.StudyInstanceUID, "",
                                    $"Send to PACS server: {node.Name}");
                                QCOperationContext.WriteSuccessRecord();
                            }
                            else
                            {
                                QCOperationContext.SetLogger(new ImportStudyLogger());
                                QCOperationContext.SetParams(userName, studyData.StudyInstanceUID, "", null);
                                QCOperationContext.WriteSuccessRecord();
                            }

                        break;
                }

                Serilog.Log.Information("Handle the CreateAndModifyStudy service end  ======================");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex.Message);
                throw new Exception(ex.Message);
            }

            return Task.FromResult(cmdResult);
        }

        #endregion

        #region Fields

        /// <summary>
        ///     DICOM命令服務
        /// </summary>
        private readonly IDcmCommand DicomCmdService;

        /// <summary>
        ///     DB Dicom Node
        /// </summary>
        private readonly DicomOperationNodeService _dicomOperationNodeService;

        /// <summary>
        ///     使用者QC操作記錄器
        /// </summary>
        private readonly QCOperationContext QCOperationContext;

        /// <summary>
        ///     Service type
        /// </summary>
        public CmdServiceType CmdServiceType { get; } = CmdServiceType.DcmData;

        #endregion
    }
}