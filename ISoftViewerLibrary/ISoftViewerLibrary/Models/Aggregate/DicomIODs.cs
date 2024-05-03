using System;
using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Events;
using ISoftViewerLibrary.Models.Exceptions;
using ISoftViewerLibrary.Models.ValueObjects;
using static ISoftViewerLibrary.Models.Entity.DicomEntities;


namespace ISoftViewerLibrary.Models.Aggregate
{
    /// <summary>
    /// DICOM 4層物件
    /// </summary>
    public class DicomIODs : AggregateRoot<DicomSourceReference>
    {
        /// <summary>
        /// 建構
        /// </summary>
        public DicomIODs()
        {
            Id = new DicomSourceReference(Guid.NewGuid());
            Studies = new List<StudyEntity>();
            Series = new Dictionary<DcmString, List<SeriesEntity>>();
            Images = new Dictionary<DcmString, List<ImageEntity>>();
        }

        #region Fields

        /// <summary>
        /// Patient層資料
        /// </summary>
        public PatientEntity Patient { get; private set; }

        /// <summary>
        /// 檢查層資料
        /// </summary>
        public List<StudyEntity> Studies { get; private set; }

        /// <summary>
        /// 系列層資料
        /// </summary>
        public Dictionary<DcmString, List<SeriesEntity>> Series { get; private set; }

        /// <summary>
        /// 影像層資料
        /// </summary>
        public Dictionary<DcmString, List<ImageEntity>> Images { get; private set; }

        /// <summary>
        /// 目前狀態
        /// </summary>
        protected DicomIODsState State;

        #endregion

        #region Methods

        /// <summary>
        /// 指定病歷資料
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public DicomIODs SetPatient(DataCorrection.V1.PatientData patient)
        {
            Apply(new DcmEvents.OnPatientCreated()
            {
                PatientId = patient.PatientId,
                NormalKeys = new DcmEvents.OnPatientUpdated
                {
                    PatientsName = patient.PatientsName,
                    PatientsSex = patient.PatientsSex,
                    PatientsBirthDate = patient.PatientsBirthDate,
                    PatientsBirthTime = patient.PatientsBirthTime,
                    OtherPatientNames = patient.OtherPatientNames,
                    OtherPatientId = patient.OtherPatientId,
                    DcmOtherTags = new List<DataCorrection.V1.DcmTagData>(patient.CustomizedFields)
                }
            });
            return this;
        }

        /// <summary>
        /// 指定檢查資料
        /// </summary>
        /// <param name="study"></param>
        /// <returns></returns>
        public DicomIODs SetStudy(DataCorrection.V1.StudyData study)
        {
            Apply(new DcmEvents.OnStudyCreated()
            {
                StudyInstanceUID = study.StudyInstanceUID,
                PatientId = study.PatientId,
                NormalKeys = new DcmEvents.OnStudyUpdated
                {
                    StudyDate = study.StudyDate,
                    StudyTime = study.StudyTime,
                    ReferringPhysiciansName = study.ReferringPhysiciansName,
                    StudyID = study.StudyID,
                    AccessionNumber = study.AccessionNumber,
                    StudyDescription = study.StudyDescription,
                    Modality = study.Modality,
                    PerformingPhysiciansName = study.PerformingPhysiciansName,
                    NameofPhysiciansReading = study.NameofPhysiciansReading,
                    ProcedureID = study.ProcedureID,
                    DcmOtherTags = new List<DataCorrection.V1.DcmTagData>(study.CustomizedFields)
                }
            });
            return this;
        }

        public DicomIODs SetScheduledProcedureStep(DataCorrection.V1.StudyData scheduled)
        {
            Apply(new DcmEvents.OnScheduledProcedureStepCreated()
            {
                StudyInstanceUID = scheduled.StudyInstanceUID,
                PatientId = scheduled.PatientId,
                StudyDate = scheduled.StudyDate,
                AccessionNumber = scheduled.AccessionNumber,
                StudyDescription = scheduled.StudyDescription,
                Modality = scheduled.Modality,
                PerformingPhysiciansName = scheduled.PerformingPhysiciansName,
                ProcedureID = scheduled.ProcedureID
            });
            return this;
        }

        /// <summary>
        /// 指定系列資料
        /// </summary>
        /// <param name="study"></param>
        /// <returns></returns>
        public DicomIODs SetSeries(DataCorrection.V1.SeriesData series)
        {
            Apply(new DcmEvents.OnSeriesCreated()
            {
                StudyInstanceUID = series.StudyInstanceUID,
                SeriesInstanceUID = series.SeriesInstanceUID,
                NormalKeys = new DcmEvents.OnSeriesUpdated()
                {
                    SeriesModality = series.SeriesModality,
                    SeriesDate = series.SeriesDate,
                    SeriesTime = series.SeriesTime,
                    SeriesNumber = series.SeriesNumber,
                    SeriesDescription = series.SeriesDescription,
                    PatientPosition = series.PatientPosition,
                    BodyPartExamined = series.BodyPartExamined,
                    DcmOtherTags = new List<DataCorrection.V1.DcmTagData>(series.CustomizedFields)
                }
            });
            return this;
        }

        /// <summary>
        /// 指定影像資料
        /// </summary>
        /// <param name="series"></param>
        /// <returns></returns>
        public DicomIODs SetImage(DataCorrection.V1.ImageBufferAndData image)
        {
            Apply(new DcmEvents.OnImageCreated()
            {
                SOPInstanceUID = image.SOPInstanceUID,
                SeriesInstanceUID = image.SeriesInstanceUID,
                SOPClassUID = image.SOPClassUID,
                IsDcmBuffer = image.Type == DataCorrection.V1.BufferType.btDcm,
                NormalKeys = new DcmEvents.OnImageUpdated
                {
                    ImageNumber = image.ImageNumber,
                    ImageDate = image.ImageDate,
                    ImageTime = image.ImageTime,
                    WindowWidth = Convert.ToString(image.WindowWidth),
                    WindowCenter = Convert.ToString(image.WindowCenter)
                },
                ImageBuffer = new DcmEvents.OnImageBufferUpdated
                {
                    ImageBuffer = image.Buffer
                }
            });
            ;
            return this;
        }

        /// <summary>
        /// 確認狀態
        /// </summary>
        protected override void EnsureValidState()
        {
            bool valid = true;
            switch (State)
            {
                case DicomIODsState.PatientCreated:
                    valid &= Patient.PatientId.Value != "" && Patient.PatientsName.Value != "";
                    break;
                case DicomIODsState.StudyCreated:
                    StudyEntity study = Studies.Last();
                    valid &= (study != null) && (study.StudyInstanceUID.Value != "" && study.Modality.Value != "");
                    break;
                case DicomIODsState.SeriesCreated:
                    // var sePair = Series.Last();
                    // valid &= (sePair.Key.Value != "" && sePair.Value.SeriesInstanceUID.Value != "" && sePair.Value.StudyInstanceUID.Value != "");
                    var sePair = Series.SelectMany(x => x.Value).ToList();
                    valid &= sePair.Any(x => x.SeriesInstanceUID.Value != "" || x.StudyInstanceUID.Value != "");
                    break;
                case DicomIODsState.ImageCreated:
                    // var imPair = Images.Last();
                    // valid &= (imPair.Key.Value != "" && imPair.Value.SeriesInstanceUID.Value != "" && imPair.Value.SOPInstanceUID.Value != "");
                    var imPair = Images.SelectMany(x => x.Value).ToList();
                    valid &= imPair.Any(x => x.SeriesInstanceUID.Value != "" || x.SOPInstanceUID.Value != "");
                    break;
                case DicomIODsState.ScheduledCreated:
                    valid &= ((Patient.PatientId.Value != string.Empty || Patient.PatientsName.Value != string.Empty) &&
                              (Studies[0].StudyDate.Value != string.Empty ||
                               Studies[0].Modality.Value != string.Empty ||
                               Studies[0].PerformingPhysiciansName.Value != string.Empty));
                    break;
            }

            if (!valid)
                throw new InvalidEntityStateException(this, $"Post-checks failed in state {State}");
        }

        /// <summary>
        /// 事件觸發
        /// </summary>
        /// <param name="event"></param>
        protected override void When(object @event)
        {
            switch (@event)
            {
                case DcmEvents.OnPatientCreated e:
                    Patient = new PatientEntity(Apply);
                    ApplyToEntity(Patient, @event: e);
                    ApplyToEntity(Patient, @event: e.NormalKeys);
                    State = DicomIODsState.PatientCreated;
                    break;
                case DcmEvents.OnStudyCreated e:
                    StudyEntity studyEntity = new(Apply);
                    ApplyToEntity(studyEntity, @event: e);
                    ApplyToEntity(studyEntity, @event: e.NormalKeys);
                    Studies.Add(studyEntity);
                    State = DicomIODsState.StudyCreated;
                    break;
                case DcmEvents.OnScheduledProcedureStepCreated e:
                    Patient = new PatientEntity(Apply);
                    ApplyToEntity(Patient, @event: e);

                    StudyEntity scheduledEntity = new(Apply);
                    ApplyToEntity(scheduledEntity, @event: e);
                    Studies.Add(scheduledEntity);
                    State = DicomIODsState.ScheduledCreated;
                    break;
                case DcmEvents.OnSeriesCreated e:
                    SeriesEntity seriesEntity = new(Apply);
                    ApplyToEntity(seriesEntity, @event: e);
                    ApplyToEntity(seriesEntity, @event: e.NormalKeys);

                    if (Series.ContainsKey(seriesEntity.StudyInstanceUID))
                        Series[seriesEntity.StudyInstanceUID].Add(seriesEntity);
                    else
                        Series.Add(seriesEntity.StudyInstanceUID, new List<SeriesEntity>() { seriesEntity });

                    State = DicomIODsState.SeriesCreated;
                    break;
                case DcmEvents.OnImageCreated e:
                    ImageEntity imageEntity = new(Apply);
                    ApplyToEntity(imageEntity, @event: e);
                    ApplyToEntity(imageEntity, @event: e.NormalKeys);
                    ApplyToEntity(imageEntity, @event: e.ImageBuffer);

                    if (Images.ContainsKey(imageEntity.SeriesInstanceUID))
                        Images[imageEntity.SeriesInstanceUID].Add(imageEntity);
                    else
                        Images.Add(imageEntity.SeriesInstanceUID, new List<ImageEntity>() { imageEntity });

                    State = DicomIODsState.ImageCreated;
                    break;
            }
        }

        /// <summary>
        /// 查詢Study層實體資料
        /// </summary>
        /// <param name="studyKey"></param>
        /// <returns></returns>
        public StudyEntity FindStudyEntity(DcmString studyKey)
        {
            var entities = Studies.Where(x => x.StudyInstanceUID.Value == studyKey.Value);
            if (entities.Any() == false)
                throw new Exception($"Can not found the StudyEntity({studyKey.Value})");

            return entities.FirstOrDefault();
        }

        /// <summary>
        /// 查詢Series層實體資料
        /// </summary>
        /// <param name="seriesKye"></param>
        /// <returns></returns>
        public SeriesEntity FindSeriesEntity(DcmString seriesKey)
        {
            var entities = Series.SelectMany(x => x.Value).ToList();
            var entity = entities.First(x => x.SeriesInstanceUID.Value == seriesKey.Value);

            if (entities.Any() == false)
                throw new Exception($"Can not found the SeriesEntity({seriesKey.Value})");

            return entity;
        }

        /// <summary>
        /// 查詢Image層實體資料
        /// </summary>
        /// <param name="imageKey"></param>
        /// <returns></returns>
        public ImageEntity FindImageEntity(DcmString imageKey)
        {
            var entities = Images.SelectMany(x => x.Value).ToList();
            var entity = entities.First(x => x.SOPInstanceUID.Value == imageKey.Value);

            if (entity == null)
                throw new Exception($"Can not found the ImageEntity({imageKey.Value})");

            return entity;
        }

        #endregion

        public enum DicomIODsState
        {
            PatientCreated,
            StudyCreated,
            SeriesCreated,
            ImageCreated,
            ScheduledCreated
        }
    }
}