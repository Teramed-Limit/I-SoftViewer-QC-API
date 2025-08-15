using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ISoftViewerQCSystem.Middleware;

public class ApiNotFoundMiddleware
{
    private readonly RequestDelegate _next;

    public ApiNotFoundMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // 只針對 /api 開頭路徑
        if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == 404)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"API endpoint not found.\"}");
        }
    }
}