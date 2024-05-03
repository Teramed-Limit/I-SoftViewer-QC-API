using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Services.RepositoryService.Interface;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PacsLogController : ControllerBase
    {
        /// <summary>
        /// 建構
        /// </summary>
        /// <param name="pacsConfig"></param>
        /// <param name="logger"></param>
        public PacsLogController(ICommonRepositoryService<SvrConfiguration> pacsConfig, ICommonRepositoryService<Log.V1.JobOptResultLog> logger)
        {
            PacsConfigDbService = (DbTableService<SvrConfiguration>)pacsConfig;
            JobOptResultDbService = (DbTableService<Log.V1.JobOptResultLog>)logger;
        }

        #region Fields        
        /// <summary>
        /// SystemConfiguration資料表處理服務
        /// </summary>
        private readonly DbTableService<SvrConfiguration> PacsConfigDbService;
        /// <summary>
        /// JobOptResultLog資料表處理服務
        /// </summary>
        private readonly DbTableService<Log.V1.JobOptResultLog> JobOptResultDbService;
        #endregion

        #region Methods
        /// <summary>
        /// 取得Log的型態
        /// </summary>
        /// <returns></returns>
        [HttpGet("pacslog/logtype")]
        public ActionResult<IEnumerable<string>> GetLogType()
        {
            List<string> result = new()
            {
                "DicomCStoreServiceProvider",
                "DicomServiceWorklistProvider",
                "DicocmServiceQRProvider",
                "ServiceJobsManager"                
            };            
            return Ok(result);
        }
        /// <summary>
        /// 取得PACS Log內容
        /// </summary>
        /// <param name="logType"></param>
        /// <param name="aeTitle"></param>
        /// <param name="studyDate"></param>
        /// <returns></returns>
        [HttpGet("pacslog")]
        public ActionResult<string> GetPacsLog(string logType, string aeTitle, string studyDate)
        {
            IEnumerable<SvrConfiguration> tmp = PacsConfigDbService.GetAll();
            SvrConfiguration config = tmp.FirstOrDefault();

            string logPath = string.Format(config.LogRootPath + @"\{0}\{1}\{2}.txt", logType, studyDate, aeTitle);
            //組合Log路徑
            //string logPath = config.LogRootPath + @"\" + logType + @"\" + studyDate + @"\" + aeTitle + ".txt";
            if (logType == "ServiceJobsManager")
                logPath = string.Format(config.LogRootPath + @"\{0}\{1}\ServiceJobsManager.txt", logType, studyDate);
            //logPath = config.LogRootPath + @"\" + logType + @"\" + studyDate + @"\ServiceJobsManager.txt";

            if (System.IO.File.Exists(logPath) == false)
                return BadRequest("Can not found the log content");
            string result = System.IO.File.ReadAllText(logPath);

            return Ok(result);
        }
        /// <summary>
        /// 取得Service Job的Log資訊
        /// </summary>
        /// <param name="patientID"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet("routingLog")]
        public ActionResult<IEnumerable<Log.V1.JobOptResultLog>> GetGetRoutingErrorLog(string patientID, string date)
        {
            if (string.IsNullOrEmpty(patientID) == true && string.IsNullOrEmpty(date) == true)
                return BadRequest("Patient ID and Date cannot be empty");

            List<PairDatas> primaryKeys = new();
            List<PairDatas> normalKeys = new();
            //若沒有值,則試為Select,若有值,則為Where
            if (string.IsNullOrEmpty(patientID) == false)            
                primaryKeys.Add(new PairDatas("PatientID", patientID, FieldType.ftString));
            else
                normalKeys.Add(new PairDatas("PatientID", patientID, FieldType.ftString));

            if (string.IsNullOrEmpty(date) == false)
                primaryKeys.Add(new PairDatas("Date", date, FieldType.ftString));
            else
                normalKeys.Add(new PairDatas("Date", date, FieldType.ftString));

            normalKeys.Add(new PairDatas("OptContent", "", FieldType.ftString));
            normalKeys.Add(new PairDatas("CallingAETitle", "", FieldType.ftString));

            return Ok(JobOptResultDbService.Get(primaryKeys, normalKeys));
        }
        #endregion
    }
}
