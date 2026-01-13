using System;
using ISoftViewerQCSystem.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ISoftViewerQCSystem
{
    /// <summary>
    /// 應用程式進入點
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 主程式進入點
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            try
            {
                var builder = CreateHostBuilder(args);
                Builder = builder;
                builder.Build().Run();
            }
            catch (Exception ex)
            {
                Log.Error("CreateHostBuilder: {Error}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 建立應用程式主機
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var env = hostingContext.HostingEnvironment;

                    // 非 Development 環境自動加密敏感配置
                    AutoEncryptionService.EnsureConfigurationEncrypted(env);

                    // 載入 appsettings.Secrets.json（存放敏感資訊，不提交到 git）
                    config.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

                    // 載入加密的配置檔（生產環境使用）
                    config.AddJsonFile("appsettings.Encrypted.json", optional: true, reloadOnChange: true);

                    // 環境變數會覆蓋所有配置（最高優先級）
                    config.AddEnvironmentVariables();

                    // 自動解密 DPAPI 加密的配置值
                    config.AddDecryptedConfiguration();
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .UseSerilog();

        public static IHostBuilder Builder { get; set; }
    }
}