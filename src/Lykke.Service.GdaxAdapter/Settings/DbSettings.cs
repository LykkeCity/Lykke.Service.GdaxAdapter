using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.GdaxAdapter.Settings
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }
    }
}
