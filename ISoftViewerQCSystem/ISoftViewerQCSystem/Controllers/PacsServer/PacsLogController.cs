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
using Microsoft.AspNetCore.Authorization;

namespace ISoftViewerQCSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        /// 允許的 Log 類型白名單 (H004 修復)
        /// </summary>
        private static readonly string[] AllowedLogTypes =
        {
            "DicomCStoreServiceProvider",
            "DicomServiceWorklistProvider",
            "DicocmServiceQRProvider",
            "ServiceJobsManager"
        };

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
            // H004 修復：驗證 logType 只允許白名單中的值
            if (!AllowedLogTypes.Contains(logType))
                return BadRequest("Invalid log type");

            // H004 修復：驗證日期格式（只允許 8 位數字，如 20260113）
            if (string.IsNullOrEmpty(studyDate) || !Regex.IsMatch(studyDate, @"^\d{8}$"))
                return BadRequest("Invalid date format");

            // H004 修復：驗證 AE Title 只允許安全字元
            if (string.IsNullOrEmpty(aeTitle) || !Regex.IsMatch(aeTitle, @"^[a-zA-Z0-9_\-]+$"))
                return BadRequest("Invalid AE Title");

            IEnumerable<SvrConfiguration> tmp = PacsConfigDbService.GetAll();
            SvrConfiguration config = tmp.FirstOrDefault();

            if (config == null || string.IsNullOrEmpty(config.LogRootPath))
                return BadRequest("Log configuration not found");

            // H004 修復：使用 Path.Combine 並驗證最終路徑在允許範圍內
            string logPath;
            if (logType == "ServiceJobsManager")
                logPath = System.IO.Path.Combine(config.LogRootPath, logType, studyDate, "ServiceJobsManager.txt");
            else
                logPath = System.IO.Path.Combine(config.LogRootPath, logType, studyDate, $"{aeTitle}.txt");

            // H004 修復：驗證最終路徑在允許的根目錄內（防止路徑遍歷）
            var fullPath = System.IO.Path.GetFullPath(logPath);
            var allowedBasePath = System.IO.Path.GetFullPath(config.LogRootPath);

            if (!fullPath.StartsWith(allowedBasePath, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid path");

            if (!System.IO.File.Exists(fullPath))
                return BadRequest("Can not found the log content");

            string result = System.IO.File.ReadAllText(fullPath);

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
