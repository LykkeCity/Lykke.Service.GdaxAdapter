using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Service.GdaxAdapter.Services.Rest;
using NUnit.Framework;

namespace Lykke.Service.GdaxAdapter.Tests
{
    [Explicit]
    public class rest_api_client_tests
    {
        [Test]
        public async Task get_products()
        {
            var products = await new GdaxClient().GetProducts();

            Console.WriteLine($"Supported products: {string.Join(", ", products.Select(x => x.Id))}");
        }
    }
}
