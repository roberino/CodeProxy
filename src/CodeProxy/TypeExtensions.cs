using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeProxy
{
    internal static class TypeExtensions
    {
        public static Task ConvertTask(this Task task, Type expectedResultType)
        {
            if (task.GetType().GetGenericArguments().First() != expectedResultType)
            {
                var result = task.GetTaskResult();

                var resultMethod = typeof(Task).GetMethod("FromResult").MakeGenericMethod(expectedResultType);

                return (Task)resultMethod.Invoke(null, new object[] { result });
            }

            return task;
        }

        public static Task ConvertToTask(this object result, Type expectedResultType)
        {
            if (expectedResultType == null) return Task.FromResult(true);

            var resultMethod = typeof(Task).GetMethod("FromResult").MakeGenericMethod(expectedResultType);

            return (Task)resultMethod.Invoke(null, new object[] { result });
        }

        public static Type CreateTaskType(Type resultType)
        {
            return typeof(Task<>).MakeGenericType(resultType);
        }

        public static object GetTaskResult(this Task task)
        {
            const string resultProperty = "Result";
            
            var rprop = task.GetType().GetProperty(resultProperty);

            return rprop.GetValue(task);
        }

        public static IEnumerable<TypeInfo> GetTypeChain(this TypeInfo type)
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

        public static IEnumerable<PropertyInfo> GetAllProperties(this TypeInfo type)
        {
            return type.GetTypeChain()
                .SelectMany(t => t.GetProperties().Where(p => p.GetMethod.IsAbstract))
                .Distinct(PropertyComparer.Instance);
        }

        public static IEnumerable<MethodInfo> GetAbstractAndVirtualMethods(this TypeInfo type)
        {
            var methods = type
                .GetTypeChain()
                .SelectMany(t => 
                    t.GetMethods()
                    .Where(m => !m.IsSpecialName && (m.IsAbstract || m.IsVirtual))
                    .OrderBy(m => m.DeclaringType.GetTypeInfo() == type ? 0 : 1))
                .Distinct(MethodComparer.Instance);

            return methods;
        }

        public static string GetTypeName(this Type type)
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

        public static string GetMethodSignature(this MethodInfo method)
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