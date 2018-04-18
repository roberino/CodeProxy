using System.Collections.Generic;

namespace CodeProxy.FailSafe
{
    public static class ClusterExtensions
    {
        public static Cluster<T> CreateCluster<T>(params T[] components)
            where T : class
        {
            return new ClusterFactory().Create(components);
        }

        public static Cluster<T> AsCluster<T>(this IEnumerable<T> components)
            where T : class
        {
            return new ClusterFactory().Create(components);
        }
    }
}
