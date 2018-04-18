using System.Collections.Generic;
using System.Reflection;

namespace CodeProxy
{
    internal class MethodComparer : IEqualityComparer<MethodInfo>
    {
        private MethodComparer()
        {
        }

        static MethodComparer()
        {
            Instance = new MethodComparer();
        }

        public static MethodComparer Instance { get; }

        public bool Equals(MethodInfo x, MethodInfo y)
        {
            return string.Equals(x.GetMethodSignature(), y.GetMethodSignature());
        }

        public int GetHashCode(MethodInfo obj)
        {
            return obj.GetMethodSignature().GetHashCode();
        }
    }
}