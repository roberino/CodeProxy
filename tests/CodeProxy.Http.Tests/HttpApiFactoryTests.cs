using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using Xunit;
using System.Threading.Tasks;

namespace CodeProxy.Http.Tests
{
    public class HttpApiFactoryTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;

        public HttpApiFactoryTests()
        {
            var host = new WebHostBuilder()
                .Configure(b =>
                {
                    b.Run(async c =>
                    {
                        await c.Response.WriteAsync("test");
                    });
                });

            _server = new TestServer(host);

            _client = _server.CreateClient();
        }

        [Fact]
        public async Task Create_WhenInvoked_ReturnsProxy()
        {
            var factory = new HttpApiFactory<ITestApi>();

            var proxy = factory.Create(_client.BaseAddress);

            var data = await proxy.GetDataAsync("test");

            Assert.Equal("test", data);
        }

        public interface ITestApi : IHttpApiClient
        {
            Task<string> GetDataAsync(string args);
        }
    }
}
