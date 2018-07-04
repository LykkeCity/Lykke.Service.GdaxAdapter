namespace Lykke.Service.GdaxAdapter.Services.Settings
{
    public sealed class OrderBookBasedFeeds
    {
        public int OrderBookDepth { get; set; }
        public RabbitMqSettings TickPrices { get; set; }
        public RabbitMqSettings OrderBooks { get; set; }
        public float MaxEventPerSecondByInstrument { get; set; }
    }
}
