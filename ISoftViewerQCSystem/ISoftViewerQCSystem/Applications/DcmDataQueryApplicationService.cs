using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AutoMapper.Configuration;
using ISoftViewerLibrary.Logics.QCOperation;
using Microsoft.Extensions.Options;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerQCSystem.Applications
{
    #region DcmDataCUDCmdApplicationService

    /// <summary>
    ///     DICOM檢查編輯應用層服務(Create,Update,Delete)
    /// </summary>
    public class DcmDataQueryApplicationService : IApplicationQueryService
    {
        /// <summary>
        ///     建構
        /// </summary>
        /// <param name="dcmQueries"></param>
        /// <param name="dicomOperationNodeService"></param>
        /// <param name="qcOperationContext"></param>
        /// <param name="settings"></param>
        public DcmDataQueryApplicationService(
            IDcmQueries dcmQueries,
            DicomOperationNodeService dicomOperationNodeService,
            QCOperationContext qcOperationContext)
        {
            DicomQryService = dcmQueries;
            QCOperationContext = qcOperationContext;
            DicomOperationNodeService = dicomOperationNodeService;
        }

        #region Methods

        /// <summary>
        ///     處理命令
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task<Queries.V1.QueryResult> Handle(string userName, object command)
        {
            Queries.V1.QueryResult jsonDatasets = null;
            try
            {
                var parameter = new Dictionary<string, object>();
                DicomIODs dcmIOD = new();
                DicomOperationNodes node;
                switch (command)
                {
                    case Queries.V1.FindWorklistKeys cmd:
                        Serilog.Log.Debug(
                            "====================== Handle the FindWorklistKeys service start ======================");
                        node = DicomOperationNodeService.GetOperationNode("Worklist", cmd.QueryName);
                        dcmIOD = new QueryDicomIODs();
                        dcmIOD.SetPatient(new PatientData
                            {
                                PatientId = cmd.PatientId,
                                PatientsName = cmd.PatientsName,
                                PatientsSex = cmd.PatientsSex,
                                PatientsBirthDate = cmd.PatientsBirthDate,
                                PatientsBirthTime = cmd.PatientsBirthTime,
                                OtherPatientNames = cmd.OtherPatientNames,
                                OtherPatientId = cmd.OtherPatientId
                            })
                            .SetStudy(new StudyData
                            {
                                StudyInstanceUID = cmd.StudyInstanceUID,
                                PatientId = cmd.PatientId,
                                StudyDate = cmd.StudyDate,
                                StudyTime = cmd.StudyTime,
                                ReferringPhysiciansName = cmd.ReferringPhysiciansName,
                                StudyID = cmd.StudyID,
                                AccessionNumber = cmd.AccessionNumber,
                                StudyDescription = cmd.StudyDescription,
                                Modality = cmd.Modality,
                                PerformingPhysiciansName = cmd.PerformingPhysiciansName,
                                NameofPhysiciansReading = cmd.NameofPhysiciansReading,
                                ProcedureID = cmd.ProcedureID
                            });
                        
                        var serializer = new XmlSerializer(typeof(WorklistReturnkeys));
                        using (var reader = new StringReader(node.CFindReqField))
                        {
                            var worklistReturnkeys = (WorklistReturnkeys)serializer.Deserialize(reader);
                            parameter.Add("worklistReturnkeys", worklistReturnkeys);
                        }
                        jsonDatasets = await DicomQryService.FindDataJson(dcmIOD, Types.DcmServiceUserType.dsutWorklist,
                            node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle, parameter);
                        // if(jsonDatasets.Datasets.Count <= 0)
                        //     throw new Exception(DicomQryService.Message);
                        Serilog.Log.Debug(
                            "====================== Handle the FindWorklistKeys service end  ======================");
                        break;
                    case Queries.V1.FindQRKeys cmd:
                        Serilog.Log.Debug(
                            "====================== Handle the FindQRKeys service start ======================");
                        node = DicomOperationNodeService.GetOperationNode("Query-Retrieve", cmd.QueryName);
                        dcmIOD = new QueryDicomIODs();
                        dcmIOD.SetPatient(new PatientData
                            {
                                PatientId = cmd.PatientId,
                                PatientsName = cmd.PatientsName,
                                PatientsSex = cmd.PatientsSex,
                                PatientsBirthDate = cmd.PatientsBirthDate,
                                PatientsBirthTime = cmd.PatientsBirthTime,
                                OtherPatientNames = cmd.OtherPatientNames,
                                OtherPatientId = cmd.OtherPatientId
                            })
                            .SetStudy(new StudyData
                            {
                                StudyInstanceUID = cmd.StudyInstanceUID,
                                PatientId = cmd.PatientId,
                                StudyDate = cmd.StudyDate,
                                StudyTime = cmd.StudyTime,
                                ReferringPhysiciansName = cmd.ReferringPhysiciansName,
                                StudyID = cmd.StudyID,
                                AccessionNumber = cmd.AccessionNumber,
                                StudyDescription = cmd.StudyDescription,
                                Modality = cmd.Modality,
                                PerformingPhysiciansName = cmd.PerformingPhysiciansName,
                                NameofPhysiciansReading = cmd.NameofPhysiciansReading,
                                ProcedureID = cmd.ProcedureID
                            })
                            .SetSeries(new SeriesData
                            {
                                SeriesInstanceUID = cmd.SeriesInstanceUID,
                                StudyInstanceUID = cmd.StudyInstanceUID,
                                SeriesModality = cmd.SeriesModality,
                                SeriesDate = cmd.SeriesDate,
                                SeriesTime = cmd.SeriesTime,
                                SeriesNumber = cmd.SeriesNumber,
                                SeriesDescription = cmd.SeriesDescription,
                                PatientPosition = cmd.PatientPosition,
                                BodyPartExamined = cmd.BodyPartExamined
                            });
                        jsonDatasets = await DicomQryService.FindDataJson(dcmIOD, Types.DcmServiceUserType.dsutFind,
                            node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle, null);
                        if (jsonDatasets == null)
                            throw new Exception(DicomQryService.Message);
                        Serilog.Log.Information(
                            "====================== Handle the FindQRKeys service end  ======================");
                        break;
                    case Queries.V1.MoveQRKeys cmd:
                        node = DicomOperationNodeService.GetOperationNode("Query-Retrieve", cmd.QueryName);
                        Serilog.Log.Information(
                            "====================== Handle the MoveQRKeys service start ======================");
                        dcmIOD = new QueryDicomIODs();
                        dcmIOD.SetPatient(new PatientData
                            {
                                PatientId = cmd.PatientId,
                            }).SetStudy(new StudyData
                            {
                                StudyInstanceUID = cmd.StudyInstanceUID,
                                PatientId = cmd.PatientId,
                            })
                            .SetSeries(new SeriesData
                            {
                                SeriesInstanceUID = cmd.SeriesInstanceUID,
                                StudyInstanceUID = cmd.StudyInstanceUID
                            });
                        
                        parameter.Add("moveAE", node.MoveAETitle);
                        jsonDatasets = await DicomQryService.FindDataJson(dcmIOD, Types.DcmServiceUserType.dsutMove,
                            node.IPAddress, node.Port, node.AETitle, node.RemoteAETitle, parameter);

                        if (jsonDatasets.FileSetIDs.Count <= 0 || jsonDatasets.FileSetIDs.FirstOrDefault() == null)
                        {
                            const string notReceivingImageMsg =
                                "Not receiving any DICOM images or server is busy to achieve.";
                            throw new Exception(!string.IsNullOrEmpty(DicomQryService.Message)
                                ? DicomQryService.Message
                                : notReceivingImageMsg);
                        }

                        Serilog.Log.Information(
                            "====================== Handle the MoveQRKeys service end  ======================");

                        // 紀錄使用者操作
                        QCOperationContext.SetLogger(new RetrieveStudyLogger());
                        QCOperationContext.SetParams(userName, cmd.StudyInstanceUID, "",
                            $"Retrieved from {node.AETitle}");
                        QCOperationContext.WriteSuccessRecord();
                        break;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, ex.Message);
                throw;
            }

            return jsonDatasets;
        }

        #endregion

        #region Fields

        /// <summary>
        ///     DICOM查詢服務
        /// </summary>
        private readonly IDcmQueries DicomQryService;

        /// <summary>
        ///     DB Dicom Node
        /// </summary>
        private readonly DicomOperationNodeService DicomOperationNodeService;

        /// <summary>
        ///     使用者QC操作記錄器
        /// </summary>
        private readonly QCOperationContext QCOperationContext;
        #endregion
    }

    #endregion
}