using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ISoftViewerQCSystem.Hubs.Dtos
{
    public class Room
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        
        [JsonPropertyName("memberList")]
        public List<RoomMember> MemberList { get; set; }
        
        private sealed class RoomEqualityComparer : IEqualityComparer<Room>
        {
            public bool Equals(Room x, Room y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id;
            }

            public int GetHashCode(Room obj)
            {
                return (obj.Id != null ? obj.Id.GetHashCode() : 0);
            }
        }
        
        public static IEqualityComparer<Room> Comparer { get; } = new RoomEqualityComparer();
    }
}