using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ISoftViewerLibrary.Logics.Interface;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Utils;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    /// <summary>
    ///     通用QC操作紀錄
    /// </summary>
    public abstract class BaseOperationLogger : IQCOperationLoggerStrategy
    {
        protected DicomStudyService DicomStudyService;
        protected OperationRecord OperationRecord;
        protected OperationRecordService OperationRecordService;
        protected IEnumerable<DicomStudyData> studyDatas = null;
        public string QCGuid { get; private set; }

        public void InjectService(OperationRecordService operationRecordService, DicomStudyService dicomStudyService)
        {
            OperationRecordService = operationRecordService;
            DicomStudyService = dicomStudyService;
        }

        public bool WriteSuccessRecord()
        {
            if (!ValidParams()) return true;
            OperationRecord.IsSuccess = 1;
            OperationRecordService.GenerateNewTransaction();
            return !OperationRecordService.Insert(OperationRecord, null);
        }

        public bool WriteFailedRecord()
        {
            if (!ValidParams()) return true;
            OperationRecord.IsSuccess = 1;
            OperationRecordService.GenerateNewTransaction();
            return !OperationRecordService.Insert(OperationRecord, null);
        }


        private bool ValidParams()
        {
            return !string.IsNullOrEmpty(OperationRecord.QCGuid) &&
                   !string.IsNullOrEmpty(OperationRecord.Operator) &&
                   !string.IsNullOrEmpty(OperationRecord.OperationName);
        }

        public void SetParams(string user, string studyInstanceUID, string reason, string desc)
        {
            // 延遲幾秒載查一次
            var retryTimes = 0;
            while ((studyDatas == null || !studyDatas.Any()))
            {
                studyDatas = DicomStudyService.Get(studyInstanceUID);
                if (studyDatas == null || !studyDatas.Any())
                    Thread.Sleep(1000);
                retryTimes++;
            }

            QCGuid = string.IsNullOrEmpty(studyDatas.First().QCGuid)
                ? GenerateQCGuid(studyInstanceUID)
                : studyDatas.First().QCGuid;

            if (!string.IsNullOrEmpty(desc)) OperationRecord.Description = desc;
            OperationRecord.QCGuid = QCGuid;
            OperationRecord.Reason = reason;
            OperationRecord.Operator = user;
        }

        public void SetReasonAndDesc(string reason, string desc)
        {
            OperationRecord.Reason = reason;
            OperationRecord.Description = desc;
        }

        private string GenerateQCGuid(string studyInstanceUID)
        {
            var tableField = new TableField();
            var guid = Guid.NewGuid().ToString();

            tableField.PrimaryFields = new()
            {
                { new PairDatas { Name = "StudyInstanceUID", Value = studyInstanceUID } },
            };

            tableField.NormalFields = new()
            {
                { new PairDatas { Name = "QCGuid", Value = guid } },
            };

            DicomStudyService.GenerateNewTransaction();
            DicomStudyService.AddOrUpdate(tableField);

            return guid;
        }
    }
}