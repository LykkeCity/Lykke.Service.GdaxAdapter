using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Common.Log;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.Tools.ObservableWebSocket;
using Lykke.Common.Log;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.GdaxAdapter.Services
{
    public sealed class GdaxOrderBookReader
    {
        public GdaxOrderBookReader(ILogFactory lf)
        {
            _log = lf.CreateLog(this);
        }

        private readonly ConcurrentDictionary<string, OrderBook> _orderBooks
            = new ConcurrentDictionary<string, OrderBook>();

        private ILog _log;
        private const string Source = "gdax";

        public OrderBook DeserializeMessage(ISocketEvent ev)
        {
            if (ev is IMessageReceived<byte[]> msg)
            {
                var json = JToken.Parse(Encoding.UTF8.GetString(msg.Content));

                if (json is JObject obj)
                {
                    switch (obj["type"].Value<string>())
                    {
                        case "snapshot":
                            return ProcessOrderBook(json.ToObject<Snapshot>());

                        case "l2update":
                            return ProcessUpdate(json.ToObject<L2Update>());
                    }

                    return null;
                }
                else
                {
                    return null;
                }
            }

            return null;
        }

        private OrderBook ProcessUpdate(L2Update update)
        {
            return _orderBooks.AddOrUpdate(update.GdaxAsset,
                _ => throw new InvalidOperationException("l2update event received before snapshot"),
                (_, ob) =>
                {
                    ob = ob.Clone(update.Timestamp);

                    foreach (var ch in update.Changes.Where(x => x[0].Value<string>() == "buy"))
                    {
                        ob.UpdateBid(ch[1].Value<decimal>(), ch[2].Value<decimal>());
                    }

                    foreach (var ch in update.Changes.Where(x => x[0].Value<string>() == "sell"))
                    {
                        ob.UpdateAsk(ch[1].Value<decimal>(), ch[2].Value<decimal>());
                    }

                    return ob;
                });
        }

        private static OrderBook ConvertSnapshot(Snapshot snapshot)
        {
            return new OrderBook(
                Source,
                ToLykkeAsset(snapshot.GdaxAsset),
                snapshot.Timestamp,
                bids: snapshot.Bids.Select(x => new OrderBookItem(x[0], x[1])),
                asks: snapshot.Asks.Select(x => new OrderBookItem(x[0], x[1]))
            );
        }

        private static string ToLykkeAsset(string gdaxAsset)
        {
            return gdaxAsset.Replace("-", "");
        }

        private OrderBook ProcessOrderBook(Snapshot snapshot)
        {
            return _orderBooks.AddOrUpdate(
                snapshot.GdaxAsset,
                _ => ConvertSnapshot(snapshot),
                (_, ob) =>
                {
                    _log.Warning($"Snapshot received twice on asset {snapshot.GdaxAsset}");
                    return ConvertSnapshot(snapshot);
                });
        }
    }
}