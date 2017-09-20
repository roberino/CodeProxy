using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeProxy
{
    internal static class TypeExtensions
    {
        internal static IEnumerable<TypeInfo> GetTypeChain(this TypeInfo type)
        {
            yield return type;

            if (type.BaseType != null)
            {
                foreach (var ifaceb in GetTypeChain(type.BaseType.GetTypeInfo()))
                {
                    yield return ifaceb;
                }
            }

            foreach (var iface in type.GetInterfaces())
            {
                var ifacet = iface.GetTypeInfo();

                yield return ifacet;

                foreach (var ifaceb in GetTypeChain(ifacet))
                {
                    yield return ifaceb;
                }
            }
        }

        internal static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo type)
        {
            return type.GetTypeChain()
                .SelectMany(t => t.GetProperties())
                .Distinct();
        }

        internal static IEnumerable<MethodInfo> GetAbstractAndVirtualMethods(this TypeInfo type)
        {
            return type
                .GetTypeChain()
                .SelectMany(t => t.GetMethods().Where(m => !m.IsSpecialName && (m.IsAbstract || m.IsVirtual)))
                .Distinct();
        }

        internal static string GetTypeName(this Type type)
        {
            if (type == typeof(void))
            {
                return "void";
            }
            else
            {
                var typeInf = type.GetTypeInfo();

                if (typeInf.IsGenericType)
                {
                    var baseName = typeInf.Name.Substring(0, typeInf.Name.IndexOf('`'));

                    var isFirst = true;

                    foreach (var tp in typeInf.GetGenericArguments())
                    {
                        string typeParamName = GetTypeName(tp);

                        if (isFirst)
                        {
                            baseName += "<";
                            isFirst = false;
                        }
                        else
                        {
                            baseName += ",";
                        }

                        baseName += typeParamName;
                    }

                    baseName += ">";

                    return baseName;
                }

                return type.Name;
            }
        }

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