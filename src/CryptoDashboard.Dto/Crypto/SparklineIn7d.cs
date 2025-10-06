using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CryptoDashboard.Dto
{
    public class SparklineIn7d
    {
        [JsonPropertyName("price")]
        public List<decimal>? Price { get; set; }
    }
}