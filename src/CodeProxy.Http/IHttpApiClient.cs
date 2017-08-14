using System;
using System.Collections.Generic;

namespace CodeProxy.Http
{
    public interface IHttpApiClient
    {
        TimeSpan DefaultTimeout { get; set; }
        Uri BaseUri { get; set; }
        IDictionary<string, string[]> GlobalHeaders { get; }
        IAuthenticationAgent AuthenticationAgent { get; set; }
    }
}
