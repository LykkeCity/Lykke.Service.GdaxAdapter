using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Lykke.Service.GdaxAdapter.Services.WebSocketClient
{
    public class SimpleSubscribe
    {
        [JsonProperty("type")]
        public string Command => "subscribe";

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<string> Assets { get; set; }

        [JsonProperty("channels")]
        public IReadOnlyCollection<string> Channels { get; set; }

        public static SimpleSubscribe CreateLevel2WithHeartbeat(IEnumerable<GdaxAsset> assets)
        {
            return new SimpleSubscribe
            {
                Assets = assets.Select(x => x.Asset).ToArray(),
                Channels = new[] {"heartbeat", "level2"}
            };
        }
    }
}