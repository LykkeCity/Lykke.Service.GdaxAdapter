using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.GdaxAdapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public GdaxAdapterSettings GdaxAdapterService { get; set; }        
    }
}
