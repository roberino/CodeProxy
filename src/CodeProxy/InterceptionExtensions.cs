using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeProxy
{
    public static class InterceptionExtensions
    {
        [ThreadStatic] private static IBuilderContext _currentBuilder;

        public static T BuildInstance<T>(this ClassFactory<T> classFactory)
            where T : class
        {
            _currentBuilder?.Dispose();
            
            var newBuilder = new BuilderContext<T>(classFactory);
            var newInstance = newBuilder.Instance;

            _currentBuilder = newBuilder;

            return newInstance;
        }

        public static void Does<T>(this T returnValue, Func<IDictionary<string, object>, T> implementation)
        {
            _currentBuilder.AddMemberImplementation(implementation);
        }

        public static void Does<T>(this T returnValue, Func<T> implementation)
        {
            _currentBuilder.AddPropertyImplementation(implementation);
        }
    }
}