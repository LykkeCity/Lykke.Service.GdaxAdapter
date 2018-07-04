using JetBrains.Annotations;
using Lykke.Service.GdaxAdapter.Services.Settings;

namespace Lykke.Service.GdaxAdapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class GdaxAdapterSettings
    {
        public DbSettings Db { get; set; }

        public OrderBookBasedFeeds Feeds { get; set; }

    }
}
