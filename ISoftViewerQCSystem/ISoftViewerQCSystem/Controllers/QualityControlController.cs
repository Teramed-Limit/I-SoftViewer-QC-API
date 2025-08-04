using System;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using AutoMapper;
using ISoftViewerLibrary.Models.BodyRequestParams;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerLibrary.Utils;
using Microsoft.Extensions.Configuration;
using ISoftViewerLibrary.Model.DicomOperator;
using Dicom;
using System.IO;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
// {
//     "tag": "(0010,0010)",
//     "fromValue": "TEST981978",
//     "toValue": "Modified^Patient"
// }

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     組態設定控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class QualityControlController : ControllerBase
    {
        private readonly DicomImagePathViewService _dicomImagePathService;
        private readonly ILogger<QualityControlController> _logger;
        private readonly DicomOperatorHelper _dicomOperatorHelper;

        public QualityControlController(DicomImagePathViewService dicomImagePathService,
            ILogger<QualityControlController> logger)
        {
            _dicomImagePathService = dicomImagePathService;
            _logger = logger;
            _dicomOperatorHelper = new DicomOperatorHelper();
        }

        [HttpPost("sopInstanceUID/{sopInstanceUID}")]
        public ActionResult ModifyTagAndApplyToImage(string sopInstanceUID, [FromBody] ImageTagModify imageTagModify)
        {
            var dicomImagePathDto = GetImageList("SOPInstanceUID", sopInstanceUID);
            if (dicomImagePathDto == null || !dicomImagePathDto.Any())
            {
                return NotFound($"No image found with SOPInstanceUID: {sopInstanceUID}");
            }

            // 呼叫修改 DICOM 標籤的方法
            return ModifyDicomTagsInFiles(dicomImagePathDto, imageTagModify);
        }

        [HttpPost("seriesInstanceUID/{seriesInstanceUID}")]
        public ActionResult ModifyTagAndApplyToSeries(string seriesInstanceUID,
            [FromBody] ImageTagModify imageTagModify)
        {
            var dicomImagePathDto = GetImageList("SeriesInstanceUID", seriesInstanceUID);
            if (dicomImagePathDto == null || !dicomImagePathDto.Any())
            {
                return NotFound($"No image found with SeriesInstanceUID: {seriesInstanceUID}");
            }

            // 呼叫修改 DICOM 標籤的方法
            return ModifyDicomTagsInFiles(dicomImagePathDto, imageTagModify);
        }

        [HttpPost("studyInstanceUID/{studyInstanceUID}")]
        public ActionResult ModifyTagAndApplyToStudy(string studyInstanceUID, [FromBody] ImageTagModify imageTagModify)
        {
            var dicomImagePathDto = GetImageList("StudyInstanceUID", studyInstanceUID);
            if (dicomImagePathDto == null || !dicomImagePathDto.Any())
            {
                return NotFound($"No image found with StudyInstanceUID: {studyInstanceUID}");
            }

            // 呼叫修改 DICOM 標籤的方法
            return ModifyDicomTagsInFiles(dicomImagePathDto, imageTagModify);
        }

        /// <summary>
        /// 修改 DICOM 文件中的標籤並保存
        /// </summary>
        /// <param name="imagePaths">DICOM 文件路徑列表</param>
        /// <param name="imageTagModify">要修改的標籤信息</param>
        /// <returns>修改結果</returns>
        private ActionResult ModifyDicomTagsInFiles(List<SearchImagePathView> imagePaths, ImageTagModify imageTagModify)
        {
            var results = new List<object>();
            var errors = new List<string>();

            foreach (var imagePath in imagePaths)
            {
                try
                {
                    string filePath = imagePath.ImageFullPath;

                    if (!System.IO.File.Exists(filePath))
                    {
                        var error = $"文件不存在: {filePath}";
                        _logger.LogWarning(error);
                        errors.Add(error);
                        continue;
                    }

                    // 打開 DICOM 文件
                    var dicomFile = DicomFile.Open(filePath);
                    if (dicomFile == null)
                    {
                        var error = $"無法打開 DICOM 文件: {filePath}";
                        _logger.LogError(error);
                        errors.Add(error);
                        continue;
                    }

                    // 解析標籤 (假設格式為 (group,element) 如 (0010,0010))
                    DicomTag tag;
                    try
                    {
                        // 解析標籤格式，假設為 "(group,element)" 或 "groupelement"
                        if (imageTagModify.Tag.Contains(","))
                        {
                            var parts = imageTagModify.Tag.Trim('(', ')').Split(',');
                            var group = Convert.ToUInt16(parts[0].Trim(), 16);
                            var element = Convert.ToUInt16(parts[1].Trim(), 16);
                            tag = new DicomTag(group, element);
                        }
                        else
                        {
                            // 假設是8位十六進制字符串格式 (例如 "00100010")
                            var tagString = imageTagModify.Tag.Replace("0x", "");
                            if (tagString.Length == 8)
                            {
                                var group = Convert.ToUInt16(tagString.Substring(0, 4), 16);
                                var element = Convert.ToUInt16(tagString.Substring(4, 4), 16);
                                tag = new DicomTag(group, element);
                            }
                            else
                            {
                                throw new FormatException("標籤格式不正確");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var error = $"無效的 DICOM 標籤格式: {imageTagModify.Tag}, 錯誤: {ex.Message}";
                        _logger.LogError(error);
                        errors.Add(error);
                        continue;
                    }

                    // 修改標籤值
                    _dicomOperatorHelper.WriteDicomValueInDataset(
                        dicomFile.Dataset, tag, imageTagModify.ToValue, true);

                    // 創建備份文件路徑（可選）
                    // string backupPath = filePath + ".backup";
                    // File.Copy(filePath, backupPath, true);

                    // 保存修改後的文件，直接覆蓋原文件
                    dicomFile.Save(filePath);

                    _logger.LogInformation(
                        "{IdentityName} 成功修改文件 {FilePath} 中的標籤 {Tag} 為 '{ToValue}'", User.Identity?.Name, filePath,
                        imageTagModify.Tag, imageTagModify.ToValue);

                    results.Add(new
                    {
                        FilePath = filePath,
                        Tag = imageTagModify.Tag,
                        OldValue = imageTagModify.FromValue,
                        NewValue = imageTagModify.ToValue,
                        Status = "成功"
                    });
                }
                catch (Exception ex)
                {
                    var error = $"{User.Identity?.Name} 修改文件 {imagePath.ImageFullPath} 時發生錯誤: {ex.Message}";
                    _logger.LogError(ex, error);
                    errors.Add(error);

                    results.Add(new
                    {
                        FilePath = imagePath.ImageFullPath,
                        Tag = imageTagModify.Tag,
                        Status = "失敗",
                        Error = ex.Message
                    });
                }
            }

            // 返回結果
            var response = new
            {
                TotalFiles = imagePaths.Count,
                SuccessCount = results.Count(r => ((dynamic)r).Status == "成功"),
                FailureCount = results.Count(r => ((dynamic)r).Status == "失敗"),
                Results = results,
                Errors = errors
            };

            if (errors.Any())
            {
                return Ok(response); // 部分成功的情況仍返回 200，但包含錯誤信息
            }

            return Ok(response);
        }

        private List<SearchImagePathView> GetImageList(string key, string value)
        {
            var where = new List<PairDatas>
            {
                new() { Name = key, Value = value }
            };

            var columns = new List<PairDatas>
            {
                new() { Name = "ImageFullPath" },
                new() { Name = key },
            };

            return _dicomImagePathService.GetSpecifyColumn(where, columns).ToList();
        }
    }
}