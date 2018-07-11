using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Lykke.Common.ExchangeAdapter;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Service.GdaxAdapter.Services.WebSocketClient;

namespace Lykke.Service.GdaxAdapter.Services
{
    public sealed class OrderBooksSession : IDisposable
    {
        public readonly IReadOnlyCollection<GdaxAsset> Instruments;
        public readonly IObservable<IReadOnlyCollection<TickPrice>> TickPrices;
        public readonly IObservable<Unit> Worker;

        private readonly Dictionary<string, IObservable<OrderBook>> _byAsset;
        private readonly CompositeDisposable _disposable;

        public OrderBooksSession(
            IReadOnlyCollection<GdaxAsset> instruments,
            IObservable<TickPrice> tickPrices,
            IObservable<OrderBook> orderBooks,
            IObservable<Unit> worker)
        {
            Instruments = instruments;
            TickPrices = CombineTickPrices(tickPrices).ShareLatest();
            Worker = worker;

            _byAsset = new Dictionary<string, IObservable<OrderBook>>(
                StringComparer.InvariantCultureIgnoreCase);

            foreach (var i in instruments)
            {
                var shareLatest = orderBooks.Where(x =>
                        string.Equals(x.Asset, i.ToLykkeAsset(), StringComparison.InvariantCultureIgnoreCase))
                    .StartWith((OrderBook) null)
                    .ShareLatest();

                _byAsset[i.ToLykkeAsset()] = shareLatest;
            }

            _disposable = new CompositeDisposable(
                _byAsset.Values
                    .Select(x => x.Subscribe())
                    .Concat(new[] {TickPrices.Subscribe()}));
        }

        private static IObservable<IReadOnlyCollection<TickPrice>> CombineTickPrices(IObservable<TickPrice> tickPrices)
        {
            return tickPrices.Scan(new ConcurrentDictionary<string, TickPrice>(),
                    (d, tp) =>
                    {
                        d[tp.Asset] = tp;
                        return d;

                    })
                .Select(x => x.Values.ToArray());
        }

        public IObservable<OrderBook> GetOrderBook(string asset)
        {
            if (_byAsset.TryGetValue(asset, out var orderBooks)) return orderBooks;
            return Observable.Empty<OrderBook>();
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
