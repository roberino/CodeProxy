using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeProxy
{
    internal sealed class InterceptorEngine<T> where T : class
    {
        private readonly IList<Func<object, PropertyInterceptionType, PropertyInfo, object, object>> _propertyInterceptors;
        private readonly IList<Func<object, MethodInfo, IDictionary<string, object>, object>> _methodInterceptors;
        private readonly IDictionary<string, PropertyInfo> _properties;
        private readonly IDictionary<string, MethodInfo> _methods;

        internal InterceptorEngine()
        {
            _propertyInterceptors = new List<Func<object, PropertyInterceptionType, PropertyInfo, object, object>>();
            _methodInterceptors = new List<Func<object, MethodInfo, IDictionary<string, object>, object>>();
            _properties = typeof(T).GetTypeInfo().GetProperties().ToDictionary(p => p.Name, p => p);
            _methods = typeof(T).GetTypeInfo().GetMethods().Where(m => !m.IsSpecialName).ToDictionary(m => m.GetMethodSignature(), m => m);
        }

        internal void Add(Func<object, PropertyInterceptionType, PropertyInfo, object, object> interceptor)
        {
            _propertyInterceptors.Add(interceptor);
        }

        internal void Add(Func<object, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _methodInterceptors.Add(interceptor);
        }

        public object InterceptGet(object instance, object value, string propName)
        {
            return InterceptProperty((int)PropertyInterceptionType.GetProperty, instance, value, propName);
        }

        public object InterceptSet(object instance, object value, string propName)
        {
            return InterceptProperty((int)PropertyInterceptionType.SetProperty, instance, value, propName);
        }

        public object InterceptMethod(object instance, IDictionary<string, object> parameters, string methodSignature)
        {
            object nextVal = null;
            var method = _methods[methodSignature];
            object val = null;

            foreach (var interceptor in _methodInterceptors)
            {
                nextVal = interceptor(instance, method, parameters);

                if (!(nextVal is ObjectConstants.Ignore)) // TODO: Not very optimised
                {
                    val = nextVal;
                }
            }

            if (val == null && Type.GetTypeCode(method.ReturnType) != TypeCode.Object)
            {
                var ctor = method.ReturnType.GetTypeInfo().GetConstructor(Type.EmptyTypes);
                val = ctor?.Invoke(null);  // return default primative type
            }

            return val;
        }

        public object InterceptProperty(int itype, object instance, object value, string propName)
        {
            object nextVal = null;
            var val = value;
            var prop = _properties[propName];

            foreach (var interceptor in _propertyInterceptors)
            {
                nextVal = interceptor(instance, (PropertyInterceptionType)itype, prop, val);

                if (!(nextVal is ObjectConstants.Ignore)) // TODO: Not very optimised
                {
                    val = nextVal;
                }
            }

            return val;
        }
    }
}