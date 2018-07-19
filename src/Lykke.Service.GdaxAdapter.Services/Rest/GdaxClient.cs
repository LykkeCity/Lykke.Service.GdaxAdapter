using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lykke.Service.GdaxAdapter.Services.Rest
{
    public class GdaxProducts
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("base_currency")]
        public string BaseCurrency { get; set; }

        [JsonProperty("quote_currency")]
        public string QuoteCurrency { get; set; }

        [JsonProperty("base_min_size")]
        public string BaseMinSize { get; set; }

        [JsonProperty("base_max_size")]
        public long? BaseMaxSize { get; set; }

        [JsonProperty("quote_increment")]
        public string QuoteIncrement { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("margin_enabled")]
        public bool? MarginEnabled { get; set; }

        [JsonProperty("status_message")]
        public object StatusMessage { get; set; }

        [JsonProperty("min_market_funds")]
        public string MinMarketFunds { get; set; }

        [JsonProperty("max_market_funds")]
        public long? MaxMarketFunds { get; set; }

        [JsonProperty("post_only")]
        public bool? PostOnly { get; set; }

        [JsonProperty("limit_only")]
        public bool? LimitOnly { get; set; }

        [JsonProperty("cancel_only")]
        public bool? CancelOnly { get; set; }
    }

    public sealed class GdaxClient
    {
        static GdaxClient()
        {
            Client = new HttpClient
            {
                BaseAddress = new Uri("https://api.pro.coinbase.com"),
                DefaultRequestHeaders =
                {
                    { "User-Agent", ".net http client"}
                }
            };
        }

        private static readonly HttpClient Client;

        public async Task<IReadOnlyCollection<GdaxProducts>> GetProducts()
        {
            using (var response = await Client.GetAsync("/products"))
            {
                // var asStr = await response.Content.ReadAsStringAsync();

                var products = await response.Content.ReadAsAsync<IReadOnlyCollection<GdaxProducts>>();

                return products.ToArray();
            }
        }
    }
}
