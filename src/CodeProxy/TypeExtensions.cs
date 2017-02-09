using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeProxy
{
    internal static class TypeExtensions
    {
        internal static string GetMethodSignature(this MethodInfo method)
        {
            return method.Name + "$" + GetParameterSig(method.GetParameters());
        }

        private static string GetParameterSig(IEnumerable<ParameterInfo> parameters)
        {
            var sb = new StringBuilder();

            foreach(var param in parameters)
            {
                sb.Append(param.Name);
                sb.Append('/');
                sb.Append(param.ParameterType.FullName);
                sb.Append(';');
            }

            return sb.ToString();
        }
    }
}