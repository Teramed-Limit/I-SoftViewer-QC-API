using System.Text.Json.Serialization;

namespace ISoftViewerQCSystem.JWT
{
    public class JwtTokenConfig
    {
        [JsonPropertyName("ValidAudience")]
        public string ValidAudience { get; set; }

        [JsonPropertyName("ValidIssuer")]
        public string ValidIssuer { get; set; }
        
    }
}
