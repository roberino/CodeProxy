using System;
using System.Collections.Generic;

namespace CodeProxy
{
    internal interface IBuilderContext : IDisposable
    {
        void AddPropertyImplementation<T>(Func<T> implementation);

        void AddMemberImplementation<T>(Func<IDictionary<string, object>, T> implementation);
    }
}