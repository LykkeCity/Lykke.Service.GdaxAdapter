using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Common.ExchangeAdapter.Server;
using Lykke.Service.GdaxAdapter.Services;

namespace Lykke.Service.GdaxAdapter.Controllers
{
    public sealed class OrderBookController : OrderBookControllerBase
    {
        public OrderBookController(OrderBookPublishingService orderBooks)
        {
            Session = orderBooks.OrderBooksSession;
        }

        protected override Common.ExchangeAdapter.Server.OrderBooksSession Session { get; }
    }

    public interface IOrderBookApi
    {
        IReadOnlyCollection<string> GetAllInstruments();
        Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices();
        Task<OrderBook> GetOrderBook(string assetPair);
    }
}
