using System;
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
    }
}
