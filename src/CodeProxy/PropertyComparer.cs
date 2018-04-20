using System.Collections.Generic;
using System.Reflection;

namespace CodeProxy
{
    internal class PropertyComparer : IEqualityComparer<PropertyInfo>
    {
        private PropertyComparer()
        {
        }

        static PropertyComparer()
        {
            Instance = new PropertyComparer();
        }

        public static PropertyComparer Instance { get; }

        public bool Equals(PropertyInfo x, PropertyInfo y)
        {
            return string.Equals(x.Name, y.Name);
        }

        public int GetHashCode(PropertyInfo obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}