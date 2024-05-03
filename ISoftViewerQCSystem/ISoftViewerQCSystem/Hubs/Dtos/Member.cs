using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ISoftViewerQCSystem.Hubs.Dtos
{
    public class RoomMember
    {
        [JsonPropertyName("isOwner")]
        public bool IsOwner { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        private sealed class RoomMemberEqualityComparer : IEqualityComparer<RoomMember>
        {
            public bool Equals(RoomMember x, RoomMember y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(RoomMember obj)
            {
                return (obj.Name != null ? obj.Name.GetHashCode() : 0);
            }
        }
        
        public static IEqualityComparer<RoomMember> Comparer { get; } = new RoomMemberEqualityComparer();
    }
}