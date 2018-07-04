using Autofac;
using Lykke.Service.GdaxAdapter.Services;
using Lykke.Service.GdaxAdapter.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.Hosting;

namespace Lykke.Service.GdaxAdapter.Modules
{    
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterInstance(_appSettings.CurrentValue.GdaxAdapterService.Feeds)
                .AsSelf();

            builder
                .RegisterType<OrderBookPublishingService>()
                .As<IHostedService>()
                .SingleInstance();

            // Do not register entire settings in container, pass necessary settings to services which requires them
        }
    }
}
