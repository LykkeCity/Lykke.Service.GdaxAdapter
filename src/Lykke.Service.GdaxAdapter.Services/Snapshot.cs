using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Service.GdaxAdapter.Services
{
    public sealed class Snapshot
    {
        [JsonProperty("product_id")]
        public string GdaxAsset { get; set; }

        [JsonProperty("time")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("bids")]
        public IReadOnlyCollection<decimal[]> Bids { get; set; }

        [JsonProperty("asks")]
        public IReadOnlyCollection<decimal[]> Asks { get; set; }
    }
}
