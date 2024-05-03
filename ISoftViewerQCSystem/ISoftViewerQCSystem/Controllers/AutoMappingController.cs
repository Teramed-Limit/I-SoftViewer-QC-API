using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Utils;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ISoftViewerQCSystem.Controllers
{
    /// <summary>
    ///     組態設定控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AutoMappingController : ControllerBase
    {
        private readonly QCAutoMappingConfigService _autoMappingConfigService;

        public AutoMappingController(QCAutoMappingConfigService qcAutoMappingConfigService, IMapper mapper)
        {
            _autoMappingConfigService = qcAutoMappingConfigService;
        }

        [HttpGet]
        public ActionResult GetAllConfig()
        {
            var result = _autoMappingConfigService.GetAll();
            var dto = result.Select(x =>
                new QCAutoMappingConfigDto
                {
                    StationName = x.StationName,
                    EnvSetup = JsonSerializer.Deserialize<EnvSetup>(x.EnvSetup, new JsonSerializerOptions()),
                    WkSCP = JsonSerializer.Deserialize<DicomNode>(x.WkSCP, new JsonSerializerOptions()),
                    StoreSCP = JsonSerializer.Deserialize<List<DicomNode>>(x.StoreSCP, new JsonSerializerOptions()),
                    CFindReqField =
                        JsonSerializer.Deserialize<ElementList>(x.CFindReqField, new JsonSerializerOptions()),
                    MappingField = JsonSerializer.Deserialize<ElementList>(x.MappingField, new JsonSerializerOptions())
                });

            return Ok(dto);
        }

        [HttpPost]
        public ActionResult AddConfig([FromBody] QCAutoMappingConfig config)
        {
            var result = _autoMappingConfigService.AddOrUpdate(config);
            if (!result) return BadRequest("Add config failed");
            return Ok(true);
        }

        [HttpPut("{stationName}")]
        public ActionResult UpdateConfig([FromBody] QCAutoMappingConfig config, string stationName)
        {
            var result = _autoMappingConfigService.AddOrUpdate(config);
            return Ok(result);
        }

        [HttpDelete("{stationName}")]
        public ActionResult DeleteConfig(string stationName)
        {
            var result = _autoMappingConfigService.Delete(stationName);
            return Ok(result);
        }
    }
}