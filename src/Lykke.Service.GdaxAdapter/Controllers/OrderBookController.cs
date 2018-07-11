using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Lykke.Common.ExchangeAdapter.Contracts;
using Lykke.Service.GdaxAdapter.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.GdaxAdapter.Controllers
{
    [Route("api/[controller]")]
    public sealed class OrderBookController : Controller, IOrderBookApi
    {
        private readonly OrderBooksSession _session;

        public OrderBookController(OrderBookPublishingService orderBooks)
        {
            _session = orderBooks.OrderBooksSession;
        }

        [SwaggerOperation("GetAllInstruments")]
        [HttpGet("GetAllInstruments")]
        public IReadOnlyCollection<string> GetAllInstruments()
        {
            return _session.Instruments.Select(x => x.ToLykkeAsset()).ToArray();
        }

        [SwaggerOperation("GetAllTickPrices")]
        [HttpGet("GetAllTickPrices")]
        public async Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices()
        {
            return await _session.TickPrices.FirstOrDefaultAsync();
        }

        [SwaggerOperation("GetOrderBook")]
        [HttpGet("GetOrderBook")]
        public async Task<OrderBook> GetOrderBook(string assetPair)
        {
            return await _session.GetOrderBook(assetPair).FirstOrDefaultAsync();
        }
    }

    public interface IOrderBookApi
    {
        IReadOnlyCollection<string> GetAllInstruments();
        Task<IReadOnlyCollection<TickPrice>> GetAllTickPrices();
        Task<OrderBook> GetOrderBook(string assetPair);
    }
}
