using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.ExchangeAdapter;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.Server;
using Lykke.Common.ExchangeAdapter.Server.Settings;
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

        private IDisposable _worker;
        public OrderBooksSession OrderBooksSession { get; private set; }

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
                            .ApplyBytesReader(reader.DeserializeMessage);
                    })
                    .ReportErrors(nameof(OrderBookPublishingService), _log)
                    .RetryWithBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(5))
                    .Share();

            var orderBooks = wsClient
                .TakeOnlyContent<OrderBook>()
                .Merge(SubscribeOnConnect(wsClient, assets).Select(_ => (OrderBook) null))
                .Where(x => x != null);


            return orderBooks.FromRawOrderBooks(
                assets.Select(x => x.ToLykkeAsset()).ToArray(),
                new OrderBookProcessingSettings
                {
                    AllowedAnomalisticAssets = new string[0],
                    MaxEventPerSecondByInstrument = _settings.MaxEventPerSecondByInstrument,
                    OrderBookDepth = _settings.OrderBookDepth,
                    OrderBooks = new RmqOutput
                    {
                        ConnectionString = _settings.OrderBooks.ConnectionString,
                        Durable = true,
                        Enabled = true,
                        Exchanger = _settings.OrderBooks.Exchanger
                    },
                    TickPrices = new RmqOutput
                    {
                        ConnectionString = _settings.TickPrices.ConnectionString,
                        Durable = true,
                        Enabled = true,
                        Exchanger = _settings.TickPrices.Exchanger
                    }
                },
                _logFactory);
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
