namespace Lykke.Service.GdaxAdapter.Services.WebSocketClient
{
    public sealed class GdaxAsset
    {
        public string Asset { get; }

        public GdaxAsset(string asset)
        {
            Asset = asset;
        }
    }
}
