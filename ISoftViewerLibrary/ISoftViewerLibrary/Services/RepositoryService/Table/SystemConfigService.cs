using System.Collections.Generic;
using System.Linq;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerQCSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ISoftViewerLibrary.Services.RepositoryService.Table;

public class SystemConfigService
{
    public SystemConfigService(IServiceScopeFactory scopeFactory)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var commonRepositoryService = scope.ServiceProvider.GetRequiredService<ICommonRepositoryService<SvrConfigurationsV2>>();
            // 初始化時從 Scoped 服務中載入資料
            PacsConfigDbServiceV2 = (DbTableService<SvrConfigurationsV2>)commonRepositoryService;
        }
        SystemConfig = PacsConfigDbServiceV2.GetAll().ToList();
    }

    private DbTableService<SvrConfigurationsV2> PacsConfigDbServiceV2 { get; set; }
    public IEnumerable<SvrConfigurationsV2> SystemConfig { get; set; }

    public IEnumerable<SvrConfigurationsV2> GetAllConfig()
    {
        var config = PacsConfigDbServiceV2.GetAll();
        return config;
    }

    public string GetConfig(string key)
    {
        var config = PacsConfigDbServiceV2.Get(key).ToList();
        return !config.Any() ? "" : config.First().Value;
    }
}