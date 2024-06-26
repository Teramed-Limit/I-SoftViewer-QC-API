﻿using ISoftViewerLibrary.Logics.Interface;
using ISoftViewerLibrary.Services.RepositoryService.Table;

namespace ISoftViewerLibrary.Logics.QCOperation
{
    public class QCOperationContext
    {
        private IQCOperationLoggerStrategy _strategy;
        private readonly OperationRecordService _operationRecordService;
        private readonly DicomStudyService _dicomStudy;

        public QCOperationContext(OperationRecordService operationRecordService, DicomStudyService dicomStudyService)
        {
            _operationRecordService = operationRecordService;
            _dicomStudy = dicomStudyService;
        }

        public void SetLogger(IQCOperationLoggerStrategy strategy)
        {
            _strategy = strategy;
            _strategy.InjectService(_operationRecordService, _dicomStudy);
        }

        public void SetParams(string user, string studyInstanceUID, string reason = "", string desc = "")
        {
            _strategy.SetParams(user, studyInstanceUID, reason, desc);
        }
        
        public string GetQCGuid()
        {
            return _strategy.QCGuid;
        }

        public void WriteSuccessRecord()
        {
            var result = _strategy.WriteSuccessRecord();
        }

        public void WriteFailedRecord(string reason, string desc)
        {
            _strategy.SetReasonAndDesc(reason, desc);
            var result = _strategy.WriteFailedRecord();
        }
    }
}