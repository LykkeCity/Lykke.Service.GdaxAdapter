using System;
using Autofac;
using Autofac.Core;
using JetBrains.Annotations;
using Lykke.Common.Log;

namespace Lykke.Service.GdaxAdapter.Client
{
    [PublicAPI]
    public static class AutofacExtension
    {
        public static void RegisterGdaxAdapterClient(this ContainerBuilder builder, string serviceUrl)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrWhiteSpace(serviceUrl))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));
            }

            builder.RegisterType<GdaxAdapterClient>()
                .WithParameter("serviceUrl", serviceUrl)
                .As<IGdaxAdapterClient>()
                .SingleInstance();
        }

        public static void RegisterGdaxAdapterClient(this ContainerBuilder builder, GdaxAdapterServiceClientSettings settings)
        {
            builder.RegisterGdaxAdapterClient(settings?.ServiceUrl);
        }
    }
}
