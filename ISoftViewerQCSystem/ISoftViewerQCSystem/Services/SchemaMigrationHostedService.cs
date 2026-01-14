using System;
using System.Threading;
using System.Threading.Tasks;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services.SchemaMigration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ISoftViewerQCSystem.Services
{
    /// <summary>
    /// Schema 遷移 Hosted Service - 在應用程式啟動時自動執行資料庫 Schema 檢查和遷移
    /// </summary>
    public class SchemaMigrationHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public SchemaMigrationHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information("開始執行資料庫 Schema 遷移檢查...");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var config = scope.ServiceProvider.GetRequiredService<EnvironmentConfiguration>();
                var schemaProvider = scope.ServiceProvider.GetService<ISchemaProvider>();

                if (schemaProvider == null)
                {
                    Log.Warning("未註冊 ISchemaProvider，跳過 Schema 遷移");
                    return;
                }

                using var migrationService = new SchemaMigrationService(
                    config.ServerName,
                    config.DatabaseName,
                    config.DBUserID,
                    config.DBPassword
                );

                // 初始化遷移系統 (建立遷移歷史表)
                migrationService.Initialize();

                // 同步資料表 Schema
                var tableDefinitions = schemaProvider.GetTableDefinitions();
                var syncSuccess = migrationService.SyncAllTables(tableDefinitions);

                if (!syncSuccess)
                {
                    Log.Warning("部分資料表 Schema 同步失敗，請檢查日誌");
                }

                // 執行自訂遷移腳本
                var customMigrations = schemaProvider.GetCustomMigrations();
                foreach (var migration in customMigrations)
                {
                    var migrationSuccess = migrationService.ExecuteCustomMigration(
                        migration.Version,
                        migration.Description,
                        migration.SqlScript
                    );

                    if (!migrationSuccess)
                    {
                        Log.Warning("自訂遷移 {Version} 執行失敗", migration.Version);
                    }
                }

                Log.Information("資料庫 Schema 遷移檢查完成");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "資料庫 Schema 遷移檢查發生錯誤");
                // 不拋出例外，讓應用程式繼續啟動
            }

            await Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
