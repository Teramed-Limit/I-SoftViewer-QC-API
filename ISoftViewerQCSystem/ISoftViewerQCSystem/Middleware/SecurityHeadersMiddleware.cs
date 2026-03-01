using System.Diagnostics.CodeAnalysis;

namespace ISoftViewerQCSystem.Middleware;

/// <summary>
/// 安全標頭中介軟體 (H003 / S7039 修復)
/// 根據路由類型套用不同的 Content-Security-Policy：
/// - API 路由 (/api/*, /swagger/*): 嚴格 CSP（default-src 'none'）
/// - SPA 路由（其他所有路徑）: 適用於 MUI/Emotion CSS-in-JS 的 CSP
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// API 路由前綴 — 這些路由回傳 JSON，不需要 inline styles
    /// </summary>
    private static readonly string[] ApiPrefixes = { "/api", "/swagger" };

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // ── 通用安全標頭（所有路由） ──

        // 防止 MIME 類型嗅探
        headers.Append("X-Content-Type-Options", "nosniff");

        // 防止點擊劫持
        headers.Append("X-Frame-Options", "DENY");

        // XSS 保護（雖然現代瀏覽器已棄用，但仍建議設置）
        headers.Append("X-XSS-Protection", "1; mode=block");

        // 控制 Referrer 資訊洩露
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // 權限策略 - 限制瀏覽器功能
        headers.Append("Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

        // 移除伺服器資訊標頭（減少資訊洩露）
        headers.Remove("Server");
        headers.Remove("X-Powered-By");

        // ── 路由分流 CSP ──

        if (IsApiRoute(context.Request.Path))
        {
            // API 路由：嚴格 CSP — JSON 回應不需要任何內嵌資源
            headers.Append("Content-Security-Policy",
                "default-src 'none'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'");
        }
        else
        {
            headers.Append("Content-Security-Policy", BuildSpaContentSecurityPolicy());
        }

        await _next(context);
    }

    /// <summary>
    /// 判斷是否為 API 路由
    /// </summary>
    private static bool IsApiRoute(PathString path)
    {
        return ApiPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 建構 SPA 路由的 Content-Security-Policy。
    /// MUI v5 + Emotion CSS-in-JS 在執行期動態注入 &lt;style&gt; 標籤，
    /// 必須允許 style-src 'unsafe-inline'。Nonce 方案無法涵蓋所有第三方元件
    /// （ag-grid、CornerstoneJS），因此此處保留 'unsafe-inline'。
    /// </summary>
    [SuppressMessage("Security", "S7039",
        Justification = "MUI v5 Emotion CSS-in-JS requires 'unsafe-inline' in style-src. " +
                        "Nonce-based approach is impractical — see plan context for details. " +
                        "API routes use strict CSP (default-src 'none').")]
    private static string BuildSpaContentSecurityPolicy()
    {
        return
            "default-src 'self' https:; " +                    // 預設允許 HTTPS 資源
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // 腳本（React 需要）
            "style-src 'self' 'unsafe-inline' https:; " +     // 樣式（MUI Emotion 需要 unsafe-inline）
            "img-src 'self' data: blob: https:; " +           // 圖片
            "font-src 'self' data: https:; " +                // 字型
            "connect-src 'self' https: wss:; " +              // API 和 WebSocket
            "worker-src 'self' blob:; " +                     // Web Workers
            "media-src 'self' https:; " +                     // 影音資源
            "object-src 'none'; " +                           // 禁止 Flash/插件
            "frame-ancestors 'none'; " +                      // 防止被嵌入 iframe
            "base-uri 'self'; " +                             // 限制 base URL
            "form-action 'self'";                             // 限制表單提交目標
    }
}

/// <summary>
/// SecurityHeadersMiddleware 擴充方法
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
