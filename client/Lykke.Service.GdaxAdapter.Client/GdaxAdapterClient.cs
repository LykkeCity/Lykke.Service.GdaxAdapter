using System;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;

namespace Lykke.Service.GdaxAdapter.Client
{
    [PublicAPI]
    public class GdaxAdapterClient : IGdaxAdapterClient, IDisposable
    {
        private readonly ILog _log;

        public GdaxAdapterClient(string serviceUrl, ILogFactory logFactory)
        {
            _log = logFactory.CreateLog(this);
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
