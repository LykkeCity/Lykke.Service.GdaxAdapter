using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.GdaxAdapter.Services
{
    public sealed class L2Update
    {
        [JsonProperty("product_id")]
        public string GdaxAsset { get; set; }

        [JsonProperty("time")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("changes")]
        public IReadOnlyCollection<JArray> Changes { get; set; }
    }
}
