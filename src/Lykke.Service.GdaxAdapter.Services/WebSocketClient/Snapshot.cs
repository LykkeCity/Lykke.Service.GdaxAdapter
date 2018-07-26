using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Service.GdaxAdapter.Services.WebSocketClient
{
    public sealed class Snapshot
    {
        [JsonProperty("product_id")]
        public string GdaxAsset { get; set; }

        [JsonProperty("time")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // string until https://github.com/JamesNK/Newtonsoft.Json/issues/1711 released (fixed 21 May 2018)
        [JsonProperty("bids")]
        public IReadOnlyCollection<string[]> Bids { get; set; }

        // string until https://github.com/JamesNK/Newtonsoft.Json/issues/1711 released (fixed 21 May 2018)
        [JsonProperty("asks")]
        public IReadOnlyCollection<string[]> Asks { get; set; }
    }
}
