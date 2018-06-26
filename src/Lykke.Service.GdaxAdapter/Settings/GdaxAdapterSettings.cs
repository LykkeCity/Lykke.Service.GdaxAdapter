using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.GdaxAdapter.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class GdaxAdapterSettings
    {
        public DbSettings Db { get; set; }
    }
}
