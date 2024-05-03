using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerLibrary.Logics.QCOperation;
using static ISoftViewerLibrary.Models.DTOs.DataCorrection.V1;

namespace ISoftViewerQCSystem.Applications
{
    /// <summary>
    /// Study QC應用層服務
    /// </summary>
    public class StudyQcApplicationService : IApplicationCmdService        
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="dbQryService"></param>
        /// <param name="dbCmdService"></param>
        /// <param name="dcmUnitOfWork"></param>
        /// <param name="dcmCqusDatasts"></param>
        /// <param name="config"></param>
        /// <param name="qcOperationContext"></param>
        public StudyQcApplicationService(ILogger<StudyQcApplicationService> logger, DbQueriesService<CustomizeTable> dbQryService, 
            DbCommandService<CustomizeTable> dbCmdService, IDcmUnitOfWork dcmUnitOfWork, IDcmCqusDatasets dcmCqusDatasts,
            EnvironmentConfiguration config, QCOperationContext qcOperationContext)
        {            
            Logger = logger;
            DbQryService = dbQryService;
            DbCmdService = dbCmdService;
            DcmUnitOfWork = dcmUnitOfWork;
            DcmCqusDatasets = dcmCqusDatasts;
            EnvirConfig = config;
            QCOperationContext = qcOperationContext;
        }

        #region Fields        
        /// <summary>
        /// 資料庫查詢服務
        /// </summary>
        private readonly DbQueriesService<CustomizeTable> DbQryService;
        /// <summary>
        /// 資料庫異動命令服務
        /// </summary>
        private readonly DbCommandService<CustomizeTable> DbCmdService;
        /// <summary>
        /// DICOM服務單一作業流程
        /// </summary>
        protected IDcmUnitOfWork DcmUnitOfWork;
        /// <summary>
        /// Dataset Repository輔助物件
        /// </summary>
        protected IDcmCqusDatasets DcmCqusDatasets;        
        /// <summary>
        /// 日誌記錄器
        /// </summary>
        private readonly ILogger<StudyQcApplicationService> Logger;
        /// <summary>
        /// 環境全域組態
        /// </summary>
        private readonly EnvironmentConfiguration EnvirConfig;
        /// <summary>
        /// Service type
        /// </summary>
        public CmdServiceType CmdServiceType { get; } = CmdServiceType.StudyQC;
        /// <summary>
        ///     使用者QC操作記錄器
        /// </summary>
        private readonly QCOperationContext QCOperationContext;
        #endregion

        #region Methods
        /// <summary>
        /// 命令處理
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task<Queries.V1.CommandResult> Handle(string userName, object command)
        {
            Queries.V1.CommandResult result = new();
            string message = string.Empty;
            try
            {
                result.ExecuteResult = true;
                switch (command)
                {
                    case MergeStudyParameter cmd:
                        using (IAsyncCommandExecutor studyMergeCmd = new QcMergeStudyCmdService(DbQryService, DbCmdService, EnvirConfig))
                        {
                            studyMergeCmd.RegistrationData(cmd);
                            studyMergeCmd.RegistrationOperationContext(QCOperationContext);
                            result.ExecuteResult = studyMergeCmd.Execute().Result;
                            message = studyMergeCmd.Message;
                        }                            
                        break;
                    case SplitStudyParameter cmd:
                        using (IAsyncCommandExecutor studySplitCmd = new QcSplitStudyCmdService(DbQryService, DbCmdService, EnvirConfig))
                        {
                            studySplitCmd.RegistrationData(cmd);
                            studySplitCmd.RegistrationOperationContext(QCOperationContext);
                            result.ExecuteResult = studySplitCmd.Execute().Result;
                            message = studySplitCmd.Message;
                        }                        
                        break;
                    case StudyMappingParameter cmd:
                        using (IAsyncCommandExecutor studyMappingCmd = new QcMappingStudyCmdService(DbQryService, DbCmdService,
                            DcmUnitOfWork, DcmCqusDatasets, EnvirConfig))
                        {
                            studyMappingCmd.RegistrationData(cmd);
                            studyMappingCmd.RegistrationOperationContext(QCOperationContext);
                            result.ExecuteResult = studyMappingCmd.Execute().Result;
                            message = studyMappingCmd.Message;
                        }
                        break;
                    case StudyUnmappingParameter cmd:
                        using (IAsyncCommandExecutor studyUnmappingCmd = new QcUnmappingStudyCdmService(DbQryService, DbCmdService,
                            DcmUnitOfWork, DcmCqusDatasets, EnvirConfig))
                        {
                            studyUnmappingCmd.RegistrationData(cmd);
                            studyUnmappingCmd.RegistrationOperationContext(QCOperationContext);
                            result.ExecuteResult = studyUnmappingCmd.Execute().Result;
                            message = studyUnmappingCmd.Message;
                        }
                        break;
                }

                result.Resultes.Add(new() { Group = 0, Elem = 0, Value = "Successfully" });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                result.Resultes.Add(new() { Group = 0, Elem = 0, Value = ex.Message });
                result.Resultes.Add(new() { Group = 0, Elem = 1, Value = message });
                result.ExecuteResult = false;
                throw new Exception(ex.Message);
            }
            return Task.FromResult(result);
        }
        #endregion
    }
}
