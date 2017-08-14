using System;
using System.Net.Http;

namespace CodeProxy.Http
{
    internal class HttpRequestParameters
    {
        public Uri Url { get; set; }

        public HttpMethod Method { get; set; }

        public HttpContent Content { get; set; }
    }
}
