using System.Net;
using System.Text.Json;
using Serilog;

namespace ISoftViewerQCSystem.Middleware;

/// <summary>
/// 全域異常處理中介軟體 (M005 修復)
/// 攔截未處理的異常，記錄詳細資訊，但只返回安全的錯誤訊息給用戶端
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 記錄完整的異常資訊（包含堆疊追蹤）
        Log.Error(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
            context.Request.Path,
            context.Request.Method);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            error = _env.IsDevelopment()
                ? exception.Message  // 開發環境顯示詳細訊息
                : "伺服器發生錯誤，請稍後再試",  // 生產環境顯示通用訊息
            code = "INTERNAL_ERROR",
            traceId = context.TraceIdentifier
        };

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

/// <summary>
/// 中介軟體擴充方法
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
