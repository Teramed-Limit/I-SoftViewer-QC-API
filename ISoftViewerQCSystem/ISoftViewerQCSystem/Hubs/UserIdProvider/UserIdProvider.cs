using Microsoft.AspNetCore.SignalR;

namespace ISoftViewerQCSystem.Hubs.UserIdProvider
{
    public class UserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            var userId = connection.User.Identity.Name;
            return userId;
        }
    }
}