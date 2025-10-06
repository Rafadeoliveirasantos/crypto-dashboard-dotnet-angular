using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CryptoDashboard.Dto.Crypto.Exchange
{
    public class ExchangeRateDto
    {
        [JsonPropertyName("rates")]
        public Dictionary<string, Dictionary<string, decimal>>? Rates { get; set; }
    }
}