using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ISoftViewerQCSystem.Applications
{
    public static class GeneralApplicationService
    {
        #region PacsSysConfigApplicationService
        /// <summary>
        /// PACS環境組態應用服務
        /// </summary>
        public class PacsSysConfigApplicationService : DbQueryApplicationService<SvrConfiguration, SvrConfiguration>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="repositoryService"></param>
            public PacsSysConfigApplicationService(ICommonRepositoryService<SvrConfiguration> repositoryService) 
                : base(repositoryService)
            {
            }

            #region Methods
            /// <summary>
            /// 轉換資料
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            protected override SvrConfiguration ConvertData(SvrConfiguration data)
            {
                data.PACSMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(data.PACSMessageWriteToLog);
                data.WorklistMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(data.WorklistMessageWriteToLog);
                data.ScheduleMessageWriteToLog = ConfigHelper.DbLogLevelToWeb(data.ScheduleMessageWriteToLog);
                return data;
            }            
            #endregion
        }
        #endregion

        #region PacsDcmProviderApplicationService
        /// <summary>
        /// PACS DicomProvider組態設定
        /// </summary>
        public class PacsDcmProviderApplicationService : DbQueryApplicationService<SvrDcmProviderWeb, SvrDcmProviderDb>
        {
            /// <summary>
            /// 建構
            /// </summary>
            /// <param name="repositoryService"></param>
            public PacsDcmProviderApplicationService(ICommonRepositoryService<SvrDcmProviderDb> repositoryService) 
                : base(repositoryService)
            {
            }

            #region Methods
            /// <summary>
            /// 產生新的轉換資料
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            protected override SvrDcmProviderWeb NewData(SvrDcmProviderDb data)
            {
                return new SvrDcmProviderWeb(data);
            }
            #endregion            
        }
        #endregion

        //#region PacsDcmNodeApplicationService
        ///// <summary>
        ///// PACS DicomNode組態設定
        ///// </summary>
        //public class PacsDcmNodeApplicationService : DbQueryApplicationService<SvrDcmNodeWeb, SvrDcmNodeDb>
        //{
        //    /// <summary>
        //    /// 建構
        //    /// </summary>
        //    /// <param name="repositoryService"></param>
        //    public PacsDcmNodeApplicationService(ICommonRepositoryService<SvrDcmNodeDb> repositoryService)
        //        : base(repositoryService)
        //    {
        //    }

        //    #region Methods
        //    /// <summary>
        //    /// 產生新的轉換資料
        //    /// </summary>
        //    /// <param name="data"></param>
        //    /// <returns></returns>
        //    protected override SvrDcmNodeWeb NewData(SvrDcmNodeDb data)
        //    {
        //        return new SvrDcmNodeWeb(data);
        //    }
        //    #endregion
        //}
        //#endregion

        #region PacsRoutingDestinationNode
        /// <summary>
        /// PACS DicomDestination組態資料
        /// </summary>
        public class PacsRoutingDestinationNode : DbQueryApplicationService<SvrDcmDestNode, SvrDcmDestNode>
        {
            public PacsRoutingDestinationNode(ICommonRepositoryService<SvrDcmDestNode> repositoryService)
                : base(repositoryService)
            {
            }

            #region Methods
            /// <summary>
            /// 取得特定資料
            /// </summary>
            /// <param name="userName"></param>
            /// <returns></returns>
            public override Task<List<string>> HandleMultiple(string userName)
            {
                List<string> result = new();
                foreach (var routingScp in TableService.GetAll())
                {
                    if (result.Contains(routingScp.AETitle) == false)
                        result.Add(routingScp.AETitle);
                }
                return Task.FromResult(result);
            }
            #endregion
        }
        #endregion
    }
}
