using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;

namespace CodeProxy.Http
{
    internal class MethodBinder
    {
        private readonly RouteBinder _routeBinder;

        public MethodBinder()
        {
            _routeBinder = new RouteBinder();
        }


        public HttpRequestMessage Bind(IHttpApiClient apiClient, MethodInfo method, IDictionary<string, object> parameters)
        {
            var route = GetRouteBinding(method);

            var url = _routeBinder.Bind(apiClient.BaseUri, route.Route, parameters);

            var message = new HttpRequestMessage()
            {
                RequestUri = url,
                Method = route.Method
            };

            foreach (var header in apiClient.GlobalHeaders)
            {
                message.Headers.Add(header.Key, header.Value);
            }

            if (route.Method.Equals(HttpMethod.Post) || route.Method.Equals(HttpMethod.Put))
            {
                var ms = new MemoryStream();
                message.Content = new StreamContent(ms);
            }

            return message;
        }

        private RouteBindingAttribute GetRouteBinding(MethodInfo method)
        {
            return (method.GetCustomAttribute(typeof(RouteBindingAttribute)) as RouteBindingAttribute) ?? new RouteBindingAttribute("");
        }
    }
}
