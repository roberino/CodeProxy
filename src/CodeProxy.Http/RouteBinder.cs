using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeProxy.Http
{
    internal class RouteBinder
    {
        public Uri Bind(Uri baseUri, string route, IDictionary<string, object> parameters)
        {
            var regex = new Regex(@"\{\w+\}");

            var path = regex.Replace(route, m => {
                return Convert(parameters[m.Value]);
            });

            return new Uri(baseUri, path);
        }

        private string Convert(object value)
        {
            if (value == null) return null;

            if (value is string) return Uri.EscapeUriString(value.ToString());

            return value.ToString();
        }
    }
}