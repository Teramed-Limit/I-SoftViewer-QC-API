using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerQCSystem.Hubs.Clients;
using ISoftViewerQCSystem.Hubs.Dtos;
using ISoftViewerQCSystem.Hubs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace ISoftViewerQCSystem.Hubs
{
    [Authorize]
    public class ChatHub : Hub<IChatClient>
    {
        // TODO: 應該可以用Observer Pattern重構
        // 用戶連線ID列表
        private static ConnectionMapping<string> _connections;

        // 用戶群組列表
        private readonly RoomService RoomService;

        public ChatHub(ConnectionMapping<string> connectionMapping)
        {
            _connections = connectionMapping;
            RoomService = new RoomService(this, _connections);
        }

        /// <summary>
        ///     連線事件
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.Identity.Name;
            // 登入就建立Room，名稱為UserId
            await RoomService.CreateRoom(userId, userId);
            // 更新連線 ID 列表
            _connections.Add(userId, Context.ConnectionId);
            // 通知在線使用者
            await Clients.All.UpdateOnlineList(_connections.GetAllUser());

            await base.OnConnectedAsync().ConfigureAwait(false);
        }

        /// <summary>
        ///     離線事件
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var userId = Context.User.Identity.Name;
            // 刪除房間
            await RoomService.DismissRoom(userId);
            // 更新連線 ID 列表
            _connections.Remove(userId, Context.ConnectionId);
            // 通知在線使用者
            await Clients.All.UpdateOnlineList(_connections.GetAllUser());

            await base.OnDisconnectedAsync(ex).ConfigureAwait(false);
        }

        /// <summary>
        ///     獲取線上帳號，除了自己以外
        /// </summary>
        public Task<List<string>> GetOnlineAccount()
        {
            var onlineUsers = _connections.GetAllUser().Where(x => x != Context.User.Identity.Name);
            return Task.FromResult(onlineUsers.ToList());
        }


        /// <summary>
        ///     獲取房間內成員
        /// </summary>
        public Task<Room> GetRoomMember(string roomId)
        {
            return Task.FromResult(RoomService.GetRoom(roomId));
        }

        /// <summary>
        ///     邀請進入遠端會診
        /// </summary>
        public async Task InviteMember(string inviter, string invitee, string roomId, string url)
        {
            await Clients.User(invitee).WaitForInvite(inviter, roomId, url);
        }

        /// <summary>
        ///     接受邀請
        /// </summary>
        public async Task AcceptInvite(string roomId)
        {
            var addedUserId = Context.User.Identity.Name;
            // 進入群組
            var room = RoomService.AddMemberToRoom(addedUserId, roomId);
            // 通知所有在此群組之使用者
            await Clients.Group(roomId).UpdateRoomMember(await room);
        }

        /// <summary>
        ///     離開群組
        /// </summary>
        public async Task LeaveRoom(string roomId, bool isOwner)
        {
            var leaveUserId = Context.User.Identity.Name;
            // 通知Session結束，所有人踢出群組
            if (isOwner)
                await Clients.Group(roomId).SessionHostLeave("Host has leave the room.");
            // 離開群組
            var room = RoomService.MemberLeaveTheRoom(leaveUserId, isOwner, roomId);
            // 通知所有在此群組之使用者
            await Clients.Group(roomId).UpdateRoomMember(await room);
        }
        
        /// <summary>
        ///     
        /// </summary>
        public async Task ViewPortSyncEvent(string eventName, string roomId, string imageId, string eventParamsJson)
        {
            try
            {
                await Clients.GroupExcept(roomId, Context.ConnectionId).ViewPortRefreshV2(imageId, eventName, eventParamsJson);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}