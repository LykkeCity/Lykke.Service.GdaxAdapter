using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Common.Log;
using Lykke.Common.ExchangeAdapter;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;

namespace Lykke.Service.GdaxAdapter.Services
{
    public static class OrderBookPipelineExtensions
    {
        private struct MidPrice
        {
            public readonly decimal Min;
            public readonly decimal Max;
            public decimal Mid => (Min + Max) / 2;
            public readonly int ItemsCount;
            public string Asset;

            public MidPrice(decimal min, decimal max, int itemsCount, string asset) : this()
            {
                Min = min;
                Max = max;
                ItemsCount = itemsCount;
                Asset = asset;
            }

            public static MidPrice? Ask(OrderBook ob)
            {
                if (ob.Asks.Any())
                {
                    return new MidPrice(
                        ob.Asks.Keys.First(),
                        ob.Asks.Keys.Last(),
                        ob.Asks.Count,
                        ob.Asset);
                }

                return null;
            }

            public static MidPrice? Bid(OrderBook ob)
            {
                if (ob.Bids.Any())
                {
                    return new MidPrice(
                        ob.Bids.Keys.First(),
                        ob.Bids.Keys.Last(),
                        ob.Bids.Count,
                        ob.Asset);
                }

                return null;
            }

            public override string ToString()
            {
                return $"{Asset}: {Mid} ({Min} .. {Max})";
            }
        }

        private static IObservable<OrderBook> DetectAndFilterAnomaliesAssumingSingleInstrument(
            this IObservable<OrderBook> source,
            ILog log)
        {
            string DetectAnomaly(MidPrice? previousMidPrice, MidPrice? midPrice, string side)
            {
                if (previousMidPrice == null) return null;
                if (midPrice == null) return null;

                if (midPrice.Value.Mid / previousMidPrice.Value.Mid > 10M
                    || previousMidPrice.Value.Mid / midPrice.Value.Mid > 10M)
                {
                    return $"Found anomaly, orderbook skipped. " +
                           $"Current {side} midPrice is " +
                           $"{previousMidPrice.Value}, the new one is {midPrice.Value}";
                }
                else
                {
                    return null;
                }
            }

            return Observable.Create<OrderBook>(async (obs, ct) =>
            {
                MidPrice? prevAsk = null;
                MidPrice? prevBid = null;

                await source.ForEachAsync(orderBook =>
                {
                    var newAskMidPrice = MidPrice.Ask(orderBook);
                    var askAnomaly = DetectAnomaly(prevAsk, newAskMidPrice, "ask");

                    var newBidMidPrice = MidPrice.Bid(orderBook);
                    var bidAnomaly = DetectAnomaly(prevBid, newBidMidPrice, "bid");

                    if (askAnomaly != null)
                    {
                        log.Warning(askAnomaly);
                    }
                    else if (bidAnomaly != null)
                    {
                        log.Warning(bidAnomaly);
                    }
                    else
                    {
                        prevAsk = newAskMidPrice ?? prevAsk;
                        prevBid = newBidMidPrice ?? prevBid;
                        obs.OnNext(orderBook);
                    }
                }, ct);
            });
        }

        public static IObservable<OrderBook> DetectAndFilterAnomalies(
            this IObservable<OrderBook> source,
            ILog log,
            IEnumerable<string> skipAssets)
        {
            var assetsToSkip = new HashSet<string>(skipAssets.Select(x => x.ToUpperInvariant()));

            return source
                .GroupBy(x => x.Asset)
                .SelectMany(group => assetsToSkip.Contains(group.Key.ToUpperInvariant())
                    ? group
                    : group.DetectAndFilterAnomaliesAssumingSingleInstrument(log));
        }

        public static IObservable<T> NeverIfNotEnabled<T>(this IObservable<T> source, bool enabled)
        {
            return enabled ? source : Observable.Never<T>();
        }

        public static IObservable<OrderBook> OnlyWithPositiveSpread(this IObservable<OrderBook> source)
        {
            return source.Where(x => !x.TryDetectNegativeSpread(out _));
        }

        public static IObservable<T> ThrottleEachInstrument<T>(
            this IObservable<T> source,
            Func<T, string> getAsset,
            float maxEventsPerSecond)
        {
            if (maxEventsPerSecond < 0) throw new ArgumentOutOfRangeException(nameof(maxEventsPerSecond));
            if (Math.Abs(maxEventsPerSecond) < 0.01) return source;

            return source
                .GroupBy(getAsset)
                .Select(grouped => grouped.Sample(TimeSpan.FromSeconds(1) / maxEventsPerSecond))
                .Merge();
        }

        public static IObservable<T> DistinctEveryInstrument<T>(this IObservable<T> source, Func<T, string> getAsset)
        {
            return source.GroupBy(getAsset).Select(x => x.DistinctUntilChanged()).Merge();
        }

        public static IObservable<Unit> PublishToRmq<T>(
            this IObservable<T> source,
            string connectionString,
            string exchanger,
            ILogFactory logFactory)
        {
            const string prefix = "lykke.";

            if (exchanger.StartsWith(prefix)) exchanger = exchanger.Substring(prefix.Length);

            var settings = RabbitMqSubscriptionSettings.CreateForPublisher(
                connectionString,
                exchanger);

            settings.IsDurable = true;

            var connection
                = new RabbitMqPublisher<T>(logFactory, settings)
                    .SetSerializer(new JsonMessageSerializer<T>())
                    .SetPublishStrategy(new DefaultFanoutPublishStrategy(settings))
                    .PublishSynchronously()
                    .Start();

            return source.SelectMany(async x =>
            {
                await connection.ProduceAsync(x);
                return Unit.Default;
            });
        }

        public static IObservable<T> ReportErrors<T>(this IObservable<T> source, string process, ILog log)
        {
            return source.Do(_ => { }, err => log.WriteWarning(process, "", "", err));
        }

        public static IObservable<Unit> ReportStatistics<T>(
            this IObservable<T> source,
            TimeSpan window,
            ILog log,
            string format = "Entities registered in the last {0}: {1}")
        {
            return source
                .WindowCount(window)
                .Sample(window)
                .Do(x => log.WriteInfo(nameof(ReportStatistics), "", string.Format(format, window, x)))
                .Select(_ => Unit.Default);
        }
    }
}
