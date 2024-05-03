using System.Collections.Generic;
using System.Threading.Tasks;
using ISoftViewerQCSystem.Hubs.Dtos;

namespace ISoftViewerQCSystem.Hubs.Clients
{
    public interface IChatClient
    {
        // 在線清單
        Task UpdateOnlineList(List<string> content);

        // 在線清單
        Task UpdateRoomMember(Room room);

        // 被邀請加入群組
        Task WaitForInvite(string inviter, string roomId, string url);
        
        // 會話擁有者離開
        Task SessionHostLeave(string message);

        // Viewport更新
        Task ViewPortRefresh(string operateUser, int viewPortIndex, Viewport viewPort, double scaleGap);
        
        // Layout更新
        Task LayoutRefresh(string operateUser, int row, int col);
        
        // Viewport更新
        Task ViewPortRefreshV2(string imageId, string eventName, string eventParamsJson);
    }
}