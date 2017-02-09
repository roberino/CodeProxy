using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeProxy
{
    public sealed class InterceptorEngine<T> where T : class
    {
        private readonly IList<Func<PropertyInfo, object, object>> _propertyInterceptors;
        private readonly IList<Func<MethodInfo, IDictionary<string, object>, object>> _methodInterceptors;
        private readonly IDictionary<string, PropertyInfo> _properties;
        private readonly IDictionary<string, MethodInfo> _methods;

        internal InterceptorEngine()
        {
            _propertyInterceptors = new List<Func<PropertyInfo, object, object>>();
            _methodInterceptors = new List<Func<MethodInfo, IDictionary<string, object>, object>>();
            _properties = typeof(T).GetTypeInfo().GetProperties().ToDictionary(p => p.Name, p => p);
            _methods = typeof(T).GetTypeInfo().GetMethods().Where(m => !m.IsSpecialName).ToDictionary(m => m.Name + "$" + GetParameterSig(m.GetParameters()), m => m);
        }

        internal void Add(Func<PropertyInfo, object, object> interceptor)
        {
            _propertyInterceptors.Add(interceptor);
        }

        internal void Add(Func<MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _methodInterceptors.Add(interceptor);
        }

        public object InterceptGet(object value, string propName)
        {
            var val = value;
            var prop = _properties[propName];

            foreach (var interceptor in _propertyInterceptors)
            {
                val = interceptor(prop, val);
            }

            return val;
        }

        public object InterceptMethod(IDictionary<string, object> parameters, string methodName)
        {
            var method = _methods[methodName + "$" + parameters.Count]; // TODO: This wont work in all cases
            object val = null;

            foreach (var interceptor in _methodInterceptors)
            {
                val = interceptor(method, parameters);
            }

            if (val == null && Type.GetTypeCode(method.ReturnType) != TypeCode.Object)
            {
                var ctor = method.ReturnType.GetTypeInfo().GetConstructor(Type.EmptyTypes);
                val = ctor?.Invoke(null);
            }

            // return primative typ

            return val;
        }

        private string GetParameterSig(IEnumerable<ParameterInfo> parameters)
        {
            return parameters.Count().ToString();
        }
    }
}