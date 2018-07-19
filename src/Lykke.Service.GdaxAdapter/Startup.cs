using System;
using JetBrains.Annotations;
using Lykke.Sdk;
using Lykke.Service.GdaxAdapter.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Service.GdaxAdapter
{
    [UsedImplicitly]
    public class Startup
    {
        private LykkeSwaggerOptions _swagger;

        [UsedImplicitly]
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {                                   
            return services.BuildServiceProvider<AppSettings>(options =>
            {
                options.Logs = logs =>
                {
                    logs.AzureTableName = "GdaxAdapterLog";
                    logs.AzureTableConnectionStringResolver = settings => settings.GdaxAdapterService.Db.LogsConnString;
                };

                _swagger = new LykkeSwaggerOptions
                {
                    ApiTitle = "GdaxAdapter"
                };
                options.SwaggerOptions = _swagger;

            });
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app)
        {
            app.UseLykkeConfiguration();

        }
    }
}
