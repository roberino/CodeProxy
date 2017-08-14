using System.Net.Http;
using System.Threading.Tasks;

namespace CodeProxy.Http
{
    internal class DefaultHttpClient : IHttpClient
    {
        private readonly HttpClient _httpClient;

        public DefaultHttpClient()
        {
            _httpClient = new HttpClient();
        }

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }
    }
}