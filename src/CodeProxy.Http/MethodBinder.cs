using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Net.Http;

namespace CodeProxy.Http
{
    internal class MethodBinder
    {
        public HttpRequestMessage Bind(Uri baseUri, MethodInfo method, IDictionary<string, object> parameters)
        {
            var route = GetRouteBinding(method);

            return new HttpRequestMessage();
        }

        private RouteBindingAttribute GetRouteBinding(MethodInfo method)
        {
            return (method.GetCustomAttribute(typeof(RouteBindingAttribute)) as RouteBindingAttribute) ?? new RouteBindingAttribute("");
        }
    }
}
