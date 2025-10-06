using System.Text.Json.Serialization;

namespace CryptoDashboard.Dto
{
    public class ImageInfo
    {
        [JsonPropertyName("thumb")]
        public string? Thumb { get; set; }

        [JsonPropertyName("small")]
        public string? Small { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }
    }
}