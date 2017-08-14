using System.Net.Http;
using System.Threading.Tasks;

namespace CodeProxy.Http
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}
