using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Text;
using Common.Log;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.Service.GdaxAdapter.Services.WebSocketClient;
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

        private readonly ILog _log;
        private const string Source = "gdax";

        public OrderBook DeserializeMessage(byte[] content)
        {
            var json = JToken.Parse(Encoding.UTF8.GetString(content));

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
                        try
                        {
                            ob.UpdateBid(ParseDecimal(ch[1].Value<string>()), ParseDecimal(ch[2].Value<string>()));
                        }
                        catch (Exception ex)
                        {
                            _log.Warning($"Error updating order book json: {ch}", ex);

                            throw;
                        }
                    }

                    foreach (var ch in update.Changes.Where(x => x[0].Value<string>() == "sell"))
                    {
                        try
                        {
                            ob.UpdateAsk(ParseDecimal(ch[1].Value<string>()), ParseDecimal(ch[2].Value<string>()));
                        }
                        catch (Exception ex)
                        {
                            _log.Warning($"Error updating order book json: {ch}", ex);

                            throw;
                        }
                    }

                    return ob;
                });
        }

        private OrderBook ConvertSnapshot(Snapshot snapshot)
        {
            return new OrderBook(
                Source,
                new GdaxAsset(snapshot.GdaxAsset).ToLykkeAsset(),
                snapshot.Timestamp,
                // string until https://github.com/JamesNK/Newtonsoft.Json/issues/1711 released (fixed 21 May 2018)
                bids: snapshot.Bids.Select(x => new OrderBookItem(ParseDecimal(x[0]), ParseDecimal(x[1]))),
                // string until https://github.com/JamesNK/Newtonsoft.Json/issues/1711 released (fixed 21 May 2018)
                asks: snapshot.Asks.Select(x => new OrderBookItem(ParseDecimal(x[0]), ParseDecimal(x[1])))
            );
        }

        // string until https://github.com/JamesNK/Newtonsoft.Json/issues/1711 released (fixed 21 May 2018)
        public static decimal ParseDecimal(string str)
        {
            return decimal.Parse(str, NumberStyles.Number | NumberStyles.Float, CultureInfo.InvariantCulture);
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
