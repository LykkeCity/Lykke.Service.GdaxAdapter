using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.ExchangeAdapter;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.Tools.ObservableWebSocket;
using Lykke.Common.Log;
using Lykke.Service.GdaxAdapter.Services.Rest;
using Lykke.Service.GdaxAdapter.Services.Settings;
using Lykke.Service.GdaxAdapter.Services.WebSocketClient;
using Microsoft.Extensions.Hosting;

namespace Lykke.Service.GdaxAdapter.Services
{
    public sealed class OrderBookPublishingService : IHostedService
    {
        private readonly ILogFactory _logFactory;
        private readonly OrderBookBasedFeeds _settings;
        private readonly ILog _log;

        public OrderBookPublishingService(
            ILogFactory logFactory,
            OrderBookBasedFeeds settings)
        {
            _logFactory = logFactory;
            _settings = settings;
            _log = logFactory.CreateLog(this);
            _log.Info("Started");
        }

        public OrderBooksSession OrderBooksSession { get; private set; }
        private IDisposable _worker;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            OrderBooksSession = await CreateWorker();
            _worker = OrderBooksSession.Worker.Subscribe();
        }

        public async Task<OrderBooksSession> CreateWorker()
        {
            var products = await new GdaxClient().GetProducts();
            var assets = products.Select(x => new GdaxAsset(x.Id)).ToArray();

            var wsClient =
                Observable.Defer(() =>
                    {
                        var reader = new GdaxOrderBookReader(_logFactory);

                        return new ObservableWebSocket("wss://ws-feed.gdax.com", Info)
                            .ApplyBytesReader(reader.DeserializeMessage)
                            .ReportErrors(nameof(OrderBookPublishingService), _log)
                            .RetryWithBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5));

                    })
                    .Share();

            var statWindow = TimeSpan.FromMinutes(1);

            var orderBooks =
                Observable.Merge(
                        wsClient.TakeOnlyContent<OrderBook>(),
                        SubscribeOnConnect(wsClient, assets).Select(_ => (OrderBook) null)
                    )
                    .Where(x => x != null)
                    .OnlyWithPositiveSpread()
                    .DetectAndFilterAnomalies(_log, new string[0])
                    .ReportErrors(nameof(OrderBookPublishingService), _log)
                    .RetryWithBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5))
                    .Share();

            var obPublisher =
                orderBooks
                    .ThrottleEachInstrument(x => x.Asset, _settings.MaxEventPerSecondByInstrument)
                    .Select(x => x.Truncate(_settings.OrderBookDepth))
                    .PublishToRmq(
                        _settings.OrderBooks.ConnectionString,
                        _settings.OrderBooks.Exchanger,
                        _logFactory)
                    .ReportErrors(nameof(OrderBookPublishingService), _log)
                    .RetryWithBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5))
                    .Share();

            var tickPrices = orderBooks
                .Select(TickPrice.FromOrderBook)
                .DistinctEveryInstrument(x => x.Asset)
                .ReportErrors(nameof(OrderBookPublishingService), _log)
                .RetryWithBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5))
                .Share();

            var tpPublisher = tickPrices
                .ThrottleEachInstrument(x => x.Asset, _settings.MaxEventPerSecondByInstrument)
                .PublishToRmq(
                    _settings.TickPrices.ConnectionString,
                    _settings.TickPrices.Exchanger,
                    _logFactory)
                .ReportErrors(nameof(OrderBookPublishingService), _log)
                .Share();

            var publishTickPrices = _settings.TickPrices.Enabled;
            var publishOrderBooks = _settings.OrderBooks.Enabled;

            var publisher = Observable.Merge(
                tpPublisher.NeverIfNotEnabled(publishTickPrices),
                obPublisher.NeverIfNotEnabled(publishOrderBooks),

                orderBooks.ReportStatistics(
                        statWindow,
                        _log,
                        "OrderBooks received from WebSocket in the last {0}: {1}")
                    .NeverIfNotEnabled(publishTickPrices || publishOrderBooks),

                tpPublisher.ReportStatistics(statWindow, _log, "TickPrices published in the last {0}: {1}")
                    .NeverIfNotEnabled(publishTickPrices),

                obPublisher.ReportStatistics(statWindow, _log, "OrderBooks published in the last {0}: {1}")
                    .NeverIfNotEnabled(publishOrderBooks)
            );

            return new OrderBooksSession(
                assets,
                tickPrices, orderBooks, publisher);
        }

        private static IObservable<Unit> SubscribeOnConnect(
            IObservable<ISocketEvent> wsClient,
            GdaxAsset[] assets)
        {
            return wsClient
                .Where(x => x is SocketConnected)
                .SelectMany(async s =>
                {
                    await s.Session.SendAsJson(SimpleSubscribe.CreateLevel2WithHeartbeat(assets));
                    return Unit.Default;
                });
        }

        private void Info(string msg)
        {
            _log.Info(msg);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _worker?.Dispose();
            return Task.CompletedTask;
        }
    }
}
