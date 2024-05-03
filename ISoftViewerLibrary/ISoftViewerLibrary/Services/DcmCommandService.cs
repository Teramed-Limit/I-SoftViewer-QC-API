using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.Interfaces;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibrary.Services
{
    #region DcmCommandService

    /// <summary>
    /// DICOM更新服務
    /// </summary>
    public class DcmCommandService : IDcmCommand
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="netUnitOfWork"></param>
        /// <param name="dcmRepository"></param>
        public DcmCommandService(IDcmUnitOfWork netUnitOfWork, IDcmRepository dcmRepository)
        {
            NetUnitOfWork = netUnitOfWork;
            DcmRepository = dcmRepository;
            NetUnitOfWork.RegisterRepository(DcmRepository);

            Message = "";
            Result = OpResult.OpSuccess;
        }

        #region Fields

        /// <summary>
        /// DICOM網路處理作業
        /// </summary>
        private readonly IDcmUnitOfWork NetUnitOfWork;

        /// <summary>
        /// DICOM處理庫
        /// </summary>
        private readonly IDcmRepository DcmRepository;

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 處理結果
        /// </summary>
        public OpResult Result { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// DICOM更新服務
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <param name="type"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        /// <param name="moveAe"></param>
        /// <returns></returns>
        public async Task<bool> Update(DicomIODs dicomIODs, DcmServiceUserType type,
            List<DicomOperationNodes> nodesList)
        {
            bool result = false;
            try
            {
                if (await DcmRepository.DcmDataEncapsulation(dicomIODs, type, null) == false)
                {
                    await NetUnitOfWork.Rollback();
                    throw new InvalidOperationException(DcmRepository.Message);
                }

                var idx = 1;
                foreach (var studyEntity in dicomIODs.Studies)
                {
                    Serilog.Log.Information("Study #{Idx}", idx);
                    Serilog.Log.Information("StudyInstanceUID: {Value}", studyEntity.StudyInstanceUID.Value);

                    if (!dicomIODs.Series.ContainsKey(studyEntity.StudyInstanceUID)) continue;
                    foreach (var seriesEntity in dicomIODs.Series[studyEntity.StudyInstanceUID])
                    {
                        Serilog.Log.Information("SeriesInstanceUID: {Value}", seriesEntity.SeriesInstanceUID.Value);

                        if (!dicomIODs.Images.ContainsKey(seriesEntity.SeriesInstanceUID)) continue;
                        foreach (var imageEntity in dicomIODs.Images[seriesEntity.SeriesInstanceUID])
                        {
                            Serilog.Log.Information("SOPInstanceUID: {Value}", imageEntity.SOPInstanceUID.Value);
                        }
                    }

                    idx++;
                }

                foreach (var node in nodesList)
                {
                    Serilog.Log.Information(
                        "Dicom service {Type} start: {NodeAeTitle}, {NodeIpAddress}, {NodeRemoteAeTitle}, {NodePort}",
                        type, node.AETitle, node.IPAddress, node.RemoteAETitle, node.Port);
                    NetUnitOfWork.Begin(node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle, type);
                    if (await NetUnitOfWork.Commit() == false)
                    {
                        result = false;
                        throw new Exception(NetUnitOfWork.Message);
                    }

                    Serilog.Log.Information("Dicom service success");
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                Serilog.Log.Error("Dicom service failed {ExMessage}", ex.Message);
            }

            return await Task.FromResult(result);
        }

        #endregion
    }

    #endregion
}