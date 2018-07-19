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

        protected override OrderBooksSession Session { get; }
    }

}
