using System;

namespace CodeProxy.FailSafe
{
    public sealed class Cluster<T>
    {
        internal Cluster(T implementation)
        {
            Implementation = implementation;
        }

        public T Implementation { get; }

        internal void RaiseError(T instance, Exception ex)
        {

        }
    }
}