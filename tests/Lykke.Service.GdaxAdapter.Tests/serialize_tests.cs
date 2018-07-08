using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter;
using Lykke.Service.GdaxAdapter.Services;
using Lykke.Service.GdaxAdapter.Services.WebSocketClient;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Lykke.Service.GdaxAdapter.Tests
{
    public sealed class serialize_tests
    {
        [Test, Explicit]
        public void test_subscribe_command()
        {
            var btcUsd = new GdaxAsset("BTC-USD");

            var cmd = SimpleSubscribe.CreateLevel2WithHeartbeat(new [] { btcUsd });

            Console.WriteLine(JsonConvert.SerializeObject(cmd));
        }

        [Test]
        public void parse_decimal()
        {
            Assert.AreEqual(234242, GdaxOrderBookReader.ParseDecimal("234242"));
            Assert.AreEqual(234242.23, GdaxOrderBookReader.ParseDecimal("234242.23"));
            Assert.AreEqual(0.00000048m, GdaxOrderBookReader.ParseDecimal("4.8e-7"));
            Assert.AreEqual(100m, GdaxOrderBookReader.ParseDecimal("1e2"));
        }
    }
}
