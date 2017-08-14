using System;
using System.Net.Http;

namespace CodeProxy.Http
{
    public class RouteBindingAttribute : Attribute
    {
        public RouteBindingAttribute(string route, HttpMethod method = null)
        {
            Route = route;
            Method = method ?? HttpMethod.Get;
        }

        public string Route { get; set; }

        public HttpMethod Method { get; set; }
    }
}
