using Microsoft.AspNetCore.SignalR;
using TeraLinkaAuth.Extensions;

namespace ISoftViewerQCSystem.Hubs.UserIdProvider;

/// <summary>
/// SignalR 使用者 ID 提供者
/// 使用 TeraLinkaAuth 的 Claims 擴展來獲取使用者 ID
/// </summary>
public class UserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // 優先使用 TeraLinkaAuth 的 GetUserId 擴展方法
        // 如果失敗，則回退到 Identity.Name
        return connection.User.GetUserId() ?? connection.User.Identity?.Name;
    }
}
