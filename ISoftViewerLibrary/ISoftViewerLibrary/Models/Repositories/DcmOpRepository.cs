using Dicom;
using ISoftViewerLibrary.Model.DicomOperator;
using ISoftViewerLibrary.Models.Aggregate;
using ISoftViewerLibrary.Models.Converter;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dicom.Network;
using static ISoftViewerLibrary.Models.Entity.DicomEntities;
using static ISoftViewerLibrary.Models.ValueObjects.Types;

namespace ISoftViewerLibrary.Models.Repositories
{
    #region DcmOpRepository

    /// <summary>
    /// DICOM處理庫
    /// </summary>
    public class DcmOpRepository : IDcmRepository
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DcmOpRepository()
        {
            DicomDatasets = new ConcurrentBag<DicomDataset>();
            Message = "";
            Result = OpResult.OpSuccess;

            DcmServiceFuncs = new Dictionary<DcmServiceUserType, Func<DicomIODs, Dictionary<string, object>, bool>>
            {
                { DcmServiceUserType.dsutStore, DcmFileProcessing },
                { DcmServiceUserType.dsutFind, DcmQueryProcessing },
                { DcmServiceUserType.dsutMove, DcmMoveProcessing },
                { DcmServiceUserType.dsutWorklist, DcmWorklistProcessing },
                { DcmServiceUserType.dsutEcho, DcmWorklistProcessing }
            };
        }

        #region Fields

        /// <summary>
        /// 處理後的DICOM檔案列表
        /// </summary>
        public ConcurrentBag<DicomDataset> DicomDatasets { get; protected set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 結果
        /// </summary>
        public OpResult Result { get; private set; }

        /// <summary>
        /// DICOM服務容器
        /// </summary>
        private readonly Dictionary<DcmServiceUserType, Func<DicomIODs, Dictionary<string, object>, bool>>
            DcmServiceFuncs;

        #endregion

        #region Methods

        /// <summary>
        /// 建立DICOM資料
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        public Task<bool> DcmDataEncapsulation(DicomIODs dicomIODs, DcmServiceUserType type,
            Dictionary<string, object> parameter)
        {
            DicomDatasets.Clear();
            bool result = false;
            try
            {
                //若傳送過來沒有影像即為查詢
                var funcPairs = DcmServiceFuncs.Where(x => x.Key == type);
                if (funcPairs.Any() == false)
                    throw new Exception($"This type {type} of service is not supported");

                var func = funcPairs.FirstOrDefault().Value;
                result = func(dicomIODs, parameter);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// 平行轉換DicomFile
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        protected bool ParallelCovertDicomFile(DicomIODs dicomIODs)
        {
            try
            {
                //同時轉換多個Image成DicomFileFormat
                Parallel.ForEach(dicomIODs.Images, pairData =>
                {
                    pairData.Value.ForEach(image =>
                    {
                        IImageBufferConverter<DicomFile> imageBufferConverter = null;
                        imageBufferConverter = image.IsDcmImageBuffer
                            ? new DcmBufferConverter()
                            : new NoneDcmBufferConverter();

                        DicomFile dcmFile = imageBufferConverter.Base64BufferToImage(image.ImageBuffer);

                        if (imageBufferConverter.Result != OpResult.OpSuccess) return;

                        using (DcmDataWrapper<ImageEntity> dcmDataWrapper = new(true, false))
                        {
                            if (dcmDataWrapper.DataWrapper(dcmFile.Dataset, image, pairData.Key) == false)
                                throw new Exception(dcmDataWrapper.Message);
                        }

                        //加入容器之中
                        DicomDatasets.Add(dcmFile.Dataset);
                    });
                });
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 轉換和修改DICOM檔案處理
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>, Dictionary<string, object> parameter
        protected bool DcmFileProcessing(DicomIODs dicomIODs, Dictionary<string, object> parameter = null)
        {
            bool result = default;
            try
            {
                //同時轉換多個Image成DicomFileFormat
                if (ParallelCovertDicomFile(dicomIODs) == false)
                    throw new Exception("Failured to parallel convert dicom format");

                DicomOperatorHelper dcmHelper = new();
                //接著處理Series
                foreach (var dcmDataset in DicomDatasets)
                {
                    DicomDataset dataset = dcmDataset;
                    DicomVR vr = DicomVR.UI;

                    //寫入Series層的資料
                    using DcmDataWrapper<SeriesEntity> seriesWrapper = new(true, false);
                    DicomTag tag = DicomTag.SeriesInstanceUID;
                    DcmString seriesKey = new(tag.Group, tag.Element,
                        dcmHelper.GetDicomValueToStringWithGroupAndElem(dataset, tag.Group, tag.Element, true), "");

                    if (seriesWrapper.DataWrapper(dataset, dicomIODs.FindSeriesEntity,
                            (parentEntity) => parentEntity.StudyInstanceUID, seriesKey) == false)
                        throw new Exception(seriesWrapper.Message);

                    //寫入Study層的資料
                    using DcmDataWrapper<StudyEntity> studyWrapper = new(true, false);
                    tag = DicomTag.StudyInstanceUID;
                    DcmString studyKey = new(tag.Group, tag.Element,
                        dcmHelper.GetDicomValueToString(dataset, tag, vr, true), "");

                    if (studyWrapper.DataWrapper(dataset, dicomIODs.FindStudyEntity,
                            (parentEntity) => parentEntity.PatientId, studyKey) == false)
                        throw new Exception(studyWrapper.Message);

                    //寫入Patient的層
                    using DcmDataWrapper<PatientEntity> patientDataWrapper = new(true, false);
                    if (patientDataWrapper.DataWrapper(dataset, dicomIODs.Patient, null) == false)
                        throw new Exception(patientDataWrapper.Message);

                    result = true;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return result;
        }

        /// <summary>
        /// DICOM QR Query Dataset
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        protected bool DcmQueryProcessing(DicomIODs dicomIODs, Dictionary<string, object> parameter = null)
        {
            var dcmDataset = GetQueryRetrieveServiceDataset(dicomIODs, false);
            return (dcmDataset != null);
        }

        /// <summary>
        /// DICOM QR Move Dataset
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        protected bool DcmMoveProcessing(DicomIODs dicomIODs, Dictionary<string, object> parameter = null)
        {
            var dcmDataset = GetQueryRetrieveServiceDataset(dicomIODs, true);
            return (dcmDataset != null);
        }

        /// <summary>
        /// DICOM Worklist Dataset
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <returns></returns>
        protected bool DcmWorklistProcessing(DicomIODs dicomIODs, Dictionary<string, object> parameter = null)
        {
            DicomDatasets.Clear();
            try
            {
                if (dicomIODs.Patient == null && dicomIODs.Studies.Any() == false)
                    throw new Exception("Worklist query condtion which cannot be empty");

                string patient_id = dicomIODs.Patient.PatientId.Value;
                string patient_name = dicomIODs.Patient.PatientsName.Value;
                string study_uid = dicomIODs.Studies[0].StudyInstanceUID.Value;
                string acc_number = dicomIODs.Studies[0].AccessionNumber.Value;
                string study_date = dicomIODs.Studies[0].StudyDate.Value;
                string p_dr_name = dicomIODs.Studies[0].PerformingPhysiciansName.Value;
                string study_des = dicomIODs.Studies[0].StudyDescription.Value;
                string modality = dicomIODs.Studies[0].Modality.Value;
                string procedure_id = dicomIODs.Studies[0].ProcedureID.Value;

                // Worklist Returnkeys
                if (parameter.ContainsKey("worklistReturnkeys"))
                {
                    DicomCFindRequest cfindRq = DicomCFindRequest.CreateWorklistQuery();
                    WorklistReturnkeys returnKeys = parameter["worklistReturnkeys"] as WorklistReturnkeys;
                    var replaceParams = new Dictionary<string, string>
                    {
                        { $"{{{DicomTag.PatientID.DictionaryEntry.Keyword.ToLower()}}}", patient_id },
                        { $"{{{DicomTag.PatientName.DictionaryEntry.Keyword.ToLower()}}}", patient_name },
                        { $"{{{DicomTag.StudyInstanceUID.DictionaryEntry.Keyword.ToLower()}}}", study_uid },
                        { $"{{{DicomTag.AccessionNumber.DictionaryEntry.Keyword.ToLower()}}}", acc_number },
                        { $"{{{DicomTag.StudyDate.DictionaryEntry.Keyword.ToLower()}}}", study_date },
                        { $"{{{DicomTag.PerformingPhysicianName.DictionaryEntry.Keyword.ToLower()}}}", p_dr_name },
                        { $"{{{DicomTag.StudyDescription.DictionaryEntry.Keyword.ToLower()}}}", study_des },
                        { $"{{{DicomTag.Modality.DictionaryEntry.Keyword.ToLower()}}}", modality },
                        { $"{{{DicomTag.RequestedProcedureID.DictionaryEntry.Keyword.ToLower()}}}", procedure_id }
                    };
                    if (returnKeys != null)
                        UpdateDatasetFromWorklist(cfindRq.Dataset, returnKeys, replaceParams);
                    DicomDatasets.Add(cfindRq.Dataset);
                }
                // Worklist default dataset
                else
                {
                    var dcmDataset = new DicomDataset
                    {
                        { DicomTag.PatientID, patient_id },
                        { DicomTag.PatientName, patient_name },
                        //dimse.Dataset.Add(DicomTag.OtherPatientIDsSequence, String.Empty);
                        { DicomTag.IssuerOfPatientID, String.Empty },
                        { DicomTag.PatientSex, String.Empty },
                        { DicomTag.PatientWeight, String.Empty },
                        { DicomTag.PatientBirthDate, String.Empty },
                        { DicomTag.MedicalAlerts, String.Empty },
                        { DicomTag.PregnancyStatus, Array.Empty<ushort>() },
                        { DicomTag.Allergies, String.Empty },
                        { DicomTag.PatientComments, String.Empty },
                        { DicomTag.SpecialNeeds, String.Empty },
                        { DicomTag.PatientState, String.Empty },
                        { DicomTag.CurrentPatientLocation, String.Empty },
                        { DicomTag.InstitutionName, String.Empty },
                        { DicomTag.AdmissionID, String.Empty },
                        { DicomTag.AccessionNumber, acc_number },
                        { DicomTag.ReferringPhysicianName, String.Empty },
                        { DicomTag.AdmittingDiagnosesDescription, String.Empty },
                        { DicomTag.RequestingPhysician, String.Empty },
                        { DicomTag.StudyInstanceUID, study_uid },
                        { DicomTag.StudyDescription, String.Empty },
                        { DicomTag.StudyID, String.Empty },
                        { DicomTag.ReasonForTheRequestedProcedure, String.Empty },
                        { DicomTag.StudyDate, String.Empty },
                        { DicomTag.StudyTime, String.Empty },
                        { DicomTag.RequestedProcedureID, procedure_id },
                        { DicomTag.RequestedProcedureDescription, String.Empty },
                        { DicomTag.RequestedProcedurePriority, String.Empty },
                        new DicomSequence(DicomTag.RequestedProcedureCodeSequence),
                        new DicomSequence(DicomTag.ReferencedStudySequence),

                        new DicomSequence(DicomTag.ProcedureCodeSequence)
                    };

                    var sps = new DicomDataset
                    {
                        { DicomTag.ScheduledStationAETitle, String.Empty },
                        { DicomTag.ScheduledStationName, String.Empty },
                        { DicomTag.ScheduledProcedureStepStartDate, study_date },
                        { DicomTag.ScheduledProcedureStepStartTime, String.Empty },
                        { DicomTag.Modality, modality },
                        { DicomTag.ScheduledPerformingPhysicianName, p_dr_name },
                        { DicomTag.ScheduledProcedureStepDescription, study_des },
                        new DicomSequence(DicomTag.ScheduledProtocolCodeSequence),
                        { DicomTag.ScheduledProcedureStepLocation, String.Empty },
                        { DicomTag.ScheduledProcedureStepID, String.Empty },
                        { DicomTag.RequestedContrastAgent, String.Empty },
                        { DicomTag.PreMedication, String.Empty },
                        { DicomTag.AnatomicalOrientationType, String.Empty }
                    };
                    dcmDataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps));
                    DicomDatasets.Add(dcmDataset);
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
                return false;
            }

            return !DicomDatasets.IsEmpty;
        }

        /// <summary>
        /// 取得Query/Retrieve Service Dataset
        /// </summary>
        /// <param name="dicomIODs"></param>
        /// <param name="isMove"></param>
        /// <returns></returns>
        protected DicomDataset GetQueryRetrieveServiceDataset(DicomIODs dicomIODs, bool isMove)
        {
            var dcmDataset = new DicomDataset();
            try
            {
                //分為二個Level
                if (dicomIODs.Series.Count > 0)
                {
                    //Series Level
                    var seriesEntity = dicomIODs.Series.First().Value.First();
                    using DcmDataWrapper<SeriesEntity> seDataWrapper = new(isMove);
                    if (seDataWrapper.DataWrapper(dcmDataset, seriesEntity, seriesEntity.StudyInstanceUID) == false)
                        throw new Exception(seDataWrapper.Message);

                    dcmDataset.AddOrUpdate(DicomTag.NumberOfSeriesRelatedInstances, "");
                    dcmDataset.AddOrUpdate(DicomTag.QueryRetrieveLevel, "SERIES");
                }
                else
                {
                    //Study Level                    
                    using DcmDataWrapper<PatientEntity> patientDataWrapper = new(isMove);
                    if (patientDataWrapper.DataWrapper(dcmDataset, dicomIODs.Patient, null) == false)
                        throw new Exception(patientDataWrapper.Message);

                    var studyEntity = dicomIODs.Studies.FirstOrDefault();
                    if (studyEntity != null)
                    {
                        using DcmDataWrapper<StudyEntity> stDataWrapper = new(isMove);
                        if (stDataWrapper.DataWrapper(dcmDataset, studyEntity, null) == false)
                            throw new Exception(stDataWrapper.Message);
                    }

                    dcmDataset.AddOrUpdate(DicomTag.QueryRetrieveLevel, "STUDY");
                }

                dcmDataset.AddOrUpdate(DicomTag.SpecificCharacterSet, "ISO_IR 192");

                DicomDatasets.Add(dcmDataset);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Result = OpResult.OpFailure;
            }

            return dcmDataset;
        }

        /// <summary>
        /// 取得Worklist查詢的Dataset
        /// </summary>
        /// <returns></returns>
        protected DicomDataset GetQueryWorklistServiceDataset()
        {
            var dcmDataset = new DicomDataset
            {
                { DicomTag.PatientID, String.Empty },
                { DicomTag.PatientName, String.Empty },
                //dimse.Dataset.Add(DicomTag.OtherPatientIDsSequence, String.Empty);
                { DicomTag.IssuerOfPatientID, String.Empty },
                { DicomTag.PatientSex, String.Empty },
                { DicomTag.PatientWeight, String.Empty },
                { DicomTag.PatientBirthDate, String.Empty },
                { DicomTag.MedicalAlerts, String.Empty },
                { DicomTag.PregnancyStatus, Array.Empty<ushort>() },
                { DicomTag.Allergies, String.Empty },
                { DicomTag.PatientComments, String.Empty },
                { DicomTag.SpecialNeeds, String.Empty },
                { DicomTag.PatientState, String.Empty },
                { DicomTag.CurrentPatientLocation, String.Empty },
                { DicomTag.InstitutionName, String.Empty },
                { DicomTag.AdmissionID, String.Empty },
                { DicomTag.AccessionNumber, String.Empty },
                { DicomTag.ReferringPhysicianName, String.Empty },
                { DicomTag.AdmittingDiagnosesDescription, String.Empty },
                { DicomTag.RequestingPhysician, String.Empty },
                { DicomTag.StudyInstanceUID, String.Empty },
                { DicomTag.StudyDescription, String.Empty },
                { DicomTag.StudyID, String.Empty },
                { DicomTag.ReasonForTheRequestedProcedure, String.Empty },
                { DicomTag.StudyDate, String.Empty },
                { DicomTag.StudyTime, String.Empty },

                { DicomTag.RequestedProcedureID, String.Empty },
                { DicomTag.RequestedProcedureDescription, String.Empty },
                { DicomTag.RequestedProcedurePriority, String.Empty },
                new DicomSequence(DicomTag.RequestedProcedureCodeSequence),
                new DicomSequence(DicomTag.ReferencedStudySequence),

                new DicomSequence(DicomTag.ProcedureCodeSequence)
            };

            var sps = new DicomDataset
            {
                { DicomTag.ScheduledStationAETitle, String.Empty },
                { DicomTag.ScheduledStationName, String.Empty },
                { DicomTag.ScheduledProcedureStepStartDate, String.Empty },
                { DicomTag.ScheduledProcedureStepStartTime, String.Empty },
                { DicomTag.Modality, String.Empty },
                { DicomTag.ScheduledPerformingPhysicianName, String.Empty },
                { DicomTag.ScheduledProcedureStepDescription, String.Empty },
                new DicomSequence(DicomTag.ScheduledProtocolCodeSequence),
                { DicomTag.ScheduledProcedureStepLocation, String.Empty },
                { DicomTag.ScheduledProcedureStepID, String.Empty },
                { DicomTag.RequestedContrastAgent, String.Empty },
                { DicomTag.PreMedication, String.Empty },
                { DicomTag.AnatomicalOrientationType, String.Empty }
            };
            dcmDataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, sps));

            return dcmDataset;
        }

        protected void UpdateDatasetFromWorklist(DicomDataset dataset, WorklistReturnkeys worklistReturnkeys,
            Dictionary<string, string> parameters)
        {
            // 處理ReturnKey元素
            foreach (var returnKey in worklistReturnkeys.ReturnKeys)
            {
                if (!string.IsNullOrEmpty(returnKey.DicomTag))
                {
                    DicomTag tag = DicomTag.Parse(returnKey.DicomTag);
                    dataset.AddOrUpdate(tag,
                        parameters.TryGetValue(returnKey.Value.ToLower(), out var parameter)
                            ? parameter
                            : returnKey.Value);
                    
                    // if (returnKey.Value == "{patientName}")
                    // {
                    //     Encoding utf8 = Encoding.Unicode;
                    //     dataset.AddOrUpdate(tag, utf8.GetString(utf8.GetBytes("中文字")));
                    //     // dataset.AddOrUpdate(tag, "幹");
                    // }
                    // else
                    // {
                    //     dataset.AddOrUpdate(tag,
                    //         parameters.TryGetValue(returnKey.Value.ToLower(), out var parameter)
                    //             ? parameter
                    //             : returnKey.Value);
                    // }
                }
            }

            // 處理ReturnKeySQ元素
            foreach (var returnKeySQ in worklistReturnkeys.ReturnKeySQs)
            {
                DicomDataset subDataset = new DicomDataset();

                foreach (var subReturnKey in returnKeySQ.ReturnKeys)
                {
                    if (!string.IsNullOrEmpty(subReturnKey.DicomTag))
                    {
                        DicomTag tag = DicomTag.Parse(subReturnKey.DicomTag);
                        // subDataset.AddOrUpdate(tag, subReturnKey.Value);
                        subDataset.AddOrUpdate(tag,
                            parameters.TryGetValue(subReturnKey.Value.ToLower(), out var parameter)
                                ? parameter
                                : subReturnKey.Value);
                    }
                }

                // 處理ReturnKeySubSQ元素
                foreach (var returnKeySubSQ in returnKeySQ.ReturnKeySubSQs)
                {
                    DicomDataset subSubDataset = new DicomDataset();

                    foreach (var subSubReturnKey in returnKeySubSQ.ReturnKeys)
                    {
                        if (!string.IsNullOrEmpty(subSubReturnKey.DicomTag))
                        {
                            DicomTag tag = DicomTag.Parse(subSubReturnKey.DicomTag);
                            // subSubDataset.AddOrUpdate(tag, subSubReturnKey.Value);
                            subSubDataset.AddOrUpdate(tag,
                                parameters.TryGetValue(subSubReturnKey.Value.ToLower(), out var parameter)
                                    ? parameter
                                    : subSubReturnKey.Value);
                        }
                    }

                    DicomTag subSequenceTag = DicomTag.Parse(returnKeySubSQ.DicomTag);
                    subDataset.AddOrUpdate(new DicomSequence(subSequenceTag, subSubDataset));
                }

                DicomTag sequenceTag = DicomTag.Parse(returnKeySQ.DicomTag);
                dataset.AddOrUpdate(new DicomSequence(sequenceTag, subDataset));
            }
        }

        #endregion
    }

    #endregion

    #region DcmSendServiceHelper

    /// <summary>
    /// DICOM CStore
    /// </summary>
    public class DcmSendServiceHelper : IDcmCqusDatasets
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DcmSendServiceHelper()
        {
            DicomDatasets = new ConcurrentBag<DicomDataset>();
        }

        #region Fields

        /// <summary>
        /// 處理的DICOM列表
        /// </summary>
        public ConcurrentBag<DicomDataset> DicomDatasets { get; set; }

        #endregion
    }

    #endregion
}