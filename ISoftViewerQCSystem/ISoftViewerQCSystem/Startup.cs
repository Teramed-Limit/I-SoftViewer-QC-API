using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using System.Xml.Serialization;
using AutoMapper;
using ISoftViewerLibrary.Applications.Interface;
using ISoftViewerLibrary.Logics.QCOperation;
using ISoftViewerLibrary.Models.DatabaseTables;
using ISoftViewerLibrary.Models.DTOs;
using ISoftViewerLibrary.Models.DTOs.PacsServer;
using ISoftViewerLibrary.Models.Interface;
using ISoftViewerLibrary.Models.Interfaces;
using ISoftViewerLibrary.Models.Repositories;
using ISoftViewerLibrary.Models.UnitOfWorks;
using ISoftViewerLibrary.Models.ValueObjects;
using ISoftViewerLibrary.Services;
using ISoftViewerLibrary.Services.RepositoryService;
using ISoftViewerLibrary.Services.RepositoryService.Interface;
using ISoftViewerLibrary.Services.RepositoryService.Table;
using ISoftViewerLibrary.Services.RepositoryService.View;
using ISoftViewerLibrary.Services.SchemaMigration;
using ISoftViewerQCSystem.Applications;
using ISoftViewerQCSystem.Hubs;
using ISoftViewerQCSystem.Hubs.Services;
using ISoftViewerQCSystem.Hubs.UserIdProvider;
using ISoftViewerQCSystem.Mapper;
using ISoftViewerQCSystem.Middleware;
using ISoftViewerQCSystem.Services;
using ISoftViewerQCSystem.utils;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TeraLinkaAuth.Extensions;
using static ISoftViewerQCSystem.Applications.GeneralApplicationService;
using Log = Serilog.Log;

namespace ISoftViewerQCSystem
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            // In production, the React files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "ClientApp/build"; });

            services.AddSingleton(p =>
            {
                var config = new EnvironmentConfiguration();
                var dbSection = Configuration.GetSection("Database");
                dbSection.Bind(config);

                var mappingTabSection = Configuration.GetSection("DcmTagMappingTable").Get<MappingTagTable>();
                config.DcmTagMappingTable = mappingTabSection;

                var mergeSplitMappingTabSection =
                    Configuration.GetSection("FieldToTagMergeSplitTable").Get<List<FieldToDcmTagMap>>();
                config.MergeSplitMappingTagTable = mergeSplitMappingTabSection;

                // var dcmSendService = Configuration.GetSection("TeramedServerDcmSendService");
                // dcmSendService.Bind(config);

                config.VirtualFilePath = Configuration.GetSection("VirtualFilePath").Value;

                config.AnsiEncoding = Configuration.GetSection("AnsiEncoding").Value;

                return config;
            });

            services.AddScoped<IApplicationQueryService, DcmDataQueryApplicationService>();
            services.AddScoped<IApplicationCmdService, DcmDataCmdApplicationService>();
            services.AddScoped<IApplicationCmdService, StudyQcApplicationService>();

            services.AddScoped<CuhkCustomizeSrvice>(); //ADD 20220330 Oscar

            services.AddScoped<IDcmCommand, DcmCommandService>();
            services.AddScoped<IDcmQueries, DcmQueriesService>();

            services.AddScoped((svc) =>
            {
                EnvironmentConfiguration envi =
                    svc.GetService(typeof(EnvironmentConfiguration)) as EnvironmentConfiguration;
                DbQueriesService<CustomizeTable> result = new(envi.DBUserID, envi.DBPassword, envi.DatabaseName,
                    envi.ServerName);
                return result;
            });
            services.AddScoped((svc) =>
            {
                EnvironmentConfiguration envi =
                    svc.GetService(typeof(EnvironmentConfiguration)) as EnvironmentConfiguration;
                DbCommandService<CustomizeTable> result = new(envi.DBUserID, envi.DBPassword, envi.DatabaseName,
                    envi.ServerName, false);
                return result;
            });

            services.AddScoped<IDcmUnitOfWork, DcmNetUnitOfWork>();
            services.AddScoped<IDcmRepository, DcmOpRepository>();
            services.AddTransient<IDcmCqusDatasets, DcmSendServiceHelper>();

            // TeraLinkaAuth 服務註冊
            services.AddTeraLinkaAuth(Configuration);
            services.AddTeraLinkaJwtAuthentication(Configuration);
            services.AddTeraLinkaAuthorization();

            services.AddSingleton<IUserIdProvider, UserIdProvider>();
            services.AddSingleton<ConnectionMapping<string>>();
            services.AddSingleton<SystemConfigService>();

            services.AddScoped<PacsDBOperationService>();
            services.AddScoped<DicomTagService>();
            services.AddScoped<LRMarkerCorrectionService>();
            services.AddScoped<QCOperationContext>();

            services.AddScoped<PacsSysConfigApplicationService>();

            services.AddScoped<ICommonRepositoryService<SvrDcmNodeDb>, DbTableService<SvrDcmNodeDb>>();
            services.AddScoped<ICommonRepositoryService<SvrDcmProviderDb>, DbTableService<SvrDcmProviderDb>>();
            services.AddScoped<ICommonRepositoryService<SvrDcmDestNode>, DbTableService<SvrDcmDestNode>>();
            services.AddScoped<ICommonRepositoryService<SvrConfiguration>, DbTableService<SvrConfiguration>>();
            services.AddScoped<ICommonRepositoryService<SvrConfigurationsV2>, DbTableService<SvrConfigurationsV2>>();
            services.AddScoped<ICommonRepositoryService<SvrFileStorageDevice>, DbTableService<SvrFileStorageDevice>>();
            services.AddScoped<ICommonRepositoryService<SvrDcmTags>, DbTableService<SvrDcmTags>>();
            services.AddScoped<ICommonRepositoryService<SvrDcmTagFilters>, DbTableService<SvrDcmTagFilters>>();
            services
                .AddScoped<ICommonRepositoryService<SvrDcmTagFilterDetail>, DbTableService<SvrDcmTagFilterDetail>>();
            services
                .AddScoped<ICommonRepositoryService<ISoftViewerLibrary.Models.DTOs.Log.V1.JobOptResultLog>,
                    DbTableService<ISoftViewerLibrary.Models.DTOs.Log.V1.JobOptResultLog>>();
            services.AddScoped<ICommonRepositoryService<SearchImagePathView>, DbTableService<SearchImagePathView>>();

            // Table Service
            services.AddScoped<DicomOperationNodeService>();
            services.AddScoped<DicomDestinationNodeService>();
            services.AddScoped<DicomPatientService>();
            services.AddScoped<DicomStudyService>();
            services.AddScoped<DicomSeriesService>();
            services.AddScoped<DicomImageService>();
            services.AddScoped<DicomImagePathViewService>();
            services.AddScoped<DicomPatientStudyViewService>();
            services.AddScoped<OperationRecordService>();
            services.AddScoped<QCOperationRecordViewService>();
            services.AddScoped<StaticOptionsService>();
            services.AddScoped<QCAutoMappingConfigService>();

            // Schema Migration 服務註冊
            services.AddSingleton<ISchemaProvider, DatabaseSchemaProvider>();
            services.AddHostedService<SchemaMigrationHostedService>();

            // services.AddAutoMapper(typeof(Startup));
            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new ServiceMappings(Configuration));
            }).CreateMapper());

            services.AddControllers(options =>
            {
                options.Conventions.Add(new RouteTokenTransformerConvention(new CamelcaseParameterTransformer()));
                // .NET 8 升級：關閉 non-nullable reference types 自動視為 Required 的行為
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "I-SoftViewer-QC-System", Version = "v1" });
                var filePath = System.IO.Path.Combine(AppContext.BaseDirectory, "ISoftViewerQCSystem.xml");

                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "JWT Authentication",
                    Description = "Enter JWT Bearer token **_only_**",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // must be lower case
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Id = JwtBearerDefaults.AuthenticationScheme,
                        Type = ReferenceType.SecurityScheme
                    }
                };
                c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { securityScheme, new string[] { } }
                });

                c.IncludeXmlComments(filePath);
            });

            var allowedOrigins = Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            // L001 修復：更明確的 CORS 策略名稱
            services.AddCors(options =>
            {
                options.AddPolicy("ProductionCors", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                        .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                        .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With")
                        .AllowCredentials();
                });
            });

            // M003 修復：添加 Rate Limiting 防止暴力破解和 DoS 攻擊
            services.AddRateLimiter(options =>
            {
                // 全域速率限制：每分鐘 100 個請求
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 10
                        }));

                // 登入端點更嚴格的限制：每分鐘 10 次
                // QueueLimit = 0 確保超過限制時立即返回 429，而不是排隊等待
                options.AddPolicy("AuthPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsync(
                        "{\"error\":\"Too many requests. Please try again later.\"}", token);
                };
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // M005 修復：全域異常處理（最早執行，攔截所有未處理的異常）
            app.UseGlobalExceptionHandler();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "I-SoftViewer-QCSystem v1"));
            }
            else
            {
                // 生產環境啟用 HSTS
                app.UseHsts();
            }

            // Security Headers - 防止 XSS、Clickjacking、MIME-sniffing 等攻擊 (H003 / S7039)
            // 根據路由分流 CSP：API 路由使用嚴格策略，SPA 路由允許 MUI Emotion 所需的 unsafe-inline
            app.UseSecurityHeaders();

            // HTTPS 重導向移到較早位置 (M006)
            app.UseHttpsRedirection();

            app.UseDefaultFiles();

            app.UseSpaStaticFiles();

            app.UseStaticFiles();

            app.UseRouting();

            // L001 修復：使用更明確的 CORS 策略名稱
            app.UseCors("ProductionCors");

            // M003 修復：啟用 Rate Limiting
            app.UseRateLimiter();

            if (env.IsDevelopment())
            {
                app.UseSerilogRequestLogging();
            }

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // L001 修復：使用更明確的 CORS 策略名稱
                endpoints.MapControllers().RequireCors("ProductionCors");
                // endpoints.MapHub<ChatHub>("/hubs/chat").RequireCors("ProductionCors");
            });
            
            // 這裡改用 MapWhen 攔截 /api 路徑，檢查是否匹配 Endpoint
            app.MapWhen(
                context => context.Request.Path.StartsWithSegments("/api"),
                apiApp =>
                {
                    apiApp.Use(async (context, next) =>
                    {
                        var endpoint = context.GetEndpoint();
                        if (endpoint == null)
                        {
                            context.Response.StatusCode = 404;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsync("{\"error\":\"API endpoint not found.\"}");
                        }
                        else
                        {
                            await next();
                        }
                    });
                }
            );

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "ClientApp";
            });
        }
    }
}