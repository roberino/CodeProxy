
using System.Reflection;

namespace CodeProxy
{
    public static class TypeExtensions
    {
        public static string GetAssemblyLoadPath(this System.Type type)
        {
            return type.GetTypeInfo().Assembly.Location;
            //return ServiceLocator.AssemblyLoader.GetAssemblyLoadPath(type.GetTypeInfo().Assembly);
        }

        public static string GetSystemAssemblyPathByName(string assemblyName)
        {
            var root = System.IO.Path.GetDirectoryName(typeof(object).GetAssemblyLoadPath());
            return System.IO.Path.Combine(@"C:\Windows\Microsoft.NET\Framework64\v4.0.30319", assemblyName);
        }
    }
}
