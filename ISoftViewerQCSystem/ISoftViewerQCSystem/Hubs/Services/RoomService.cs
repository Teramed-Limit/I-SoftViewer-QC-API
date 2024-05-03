using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ISoftViewerQCSystem.Hubs.Dtos;

namespace ISoftViewerQCSystem.Hubs.Services
{
    public class RoomService
    {
        // 用戶群組列表
        private static List<Room> _roomList = new();

        private static ConnectionMapping<string> _connections;

        private readonly ChatHub _hub;

        public RoomService(ChatHub hub, ConnectionMapping<string> connections)
        {
            _hub = hub;
            _connections = connections;
        }

        public async Task CreateRoom(string roomName, string userId)
        {
            // 登入就建立Room，名稱為UserId
            var room = new Room() { Id = roomName, MemberList = new List<RoomMember>() };
            var member = new RoomMember() { IsOwner = true, Name = userId };
            room.MemberList.Add(member);
            room.MemberList = room.MemberList.Distinct(RoomMember.Comparer).ToList();
            _roomList.Add(room);
            _roomList = _roomList.Distinct(Room.Comparer).ToList();

            // 建立Group
            await _hub.Groups.AddToGroupAsync(_hub.Context.ConnectionId, userId);
        }

        public async Task DismissRoom(string userId)
        {
            var room = _roomList.First(x => x.Id == userId);
            // 移除Group
            foreach (var connId in room.MemberList.SelectMany(member =>
                         _connections.GetUserConnectionIdList(member.Name)))
            {
                await _hub.Groups.RemoveFromGroupAsync(connId, userId);
            }

            _roomList.Remove(_roomList.First(x => x.Id == userId));
        }

        public async Task<Room> AddMemberToRoom(string memberUserId, string roomId)
        {
            var room = GetRoom(roomId);
            var member = new RoomMember() { IsOwner = false, Name = memberUserId };
            room.MemberList.Add(member);
            room.MemberList = room.MemberList.Distinct(RoomMember.Comparer).ToList();

            // 進入群組
            await _hub.Groups.AddToGroupAsync(_hub.Context.ConnectionId, roomId);
            return room;
        }

        public async Task<Room> MemberLeaveTheRoom(string userId, bool isOwner, string roomId)
        {
            var room = GetRoom(roomId);
            // 房主刪除其他人即可
            if (isOwner)
            {
                // 從群組找出不是房主的Connection Id全數刪除
                foreach (var connectionIdList in from member in room.MemberList
                         let connectionIdList = _connections.GetUserConnectionIdList(member.Name)
                         where userId != member.Name
                         select connectionIdList)
                {
                    foreach (var connId in connectionIdList)
                        await _hub.Groups.RemoveFromGroupAsync(connId, roomId);
                }

                room.MemberList.RemoveAll((s => s.Name != userId));
            }
            // 房客刪除
            else
            {
                // 從群組找出Connection Id全數刪除
                var connectionIdList = _connections.GetUserConnectionIdList(userId);
                foreach (var connId in connectionIdList)
                    await _hub.Groups.RemoveFromGroupAsync(connId, roomId);
                
                room.MemberList.RemoveAll((s => s.Name == userId));
            }
            return room;
        }

        public static Room GetRoom(string roomId)
        {
            return _roomList.First(x => x.Id == roomId);
        }

        public List<RoomMember> GetRoomMember(string roomId)
        {
            return _roomList.First(x => x.Id == roomId).MemberList;
        }
    }
}