using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeProxy
{
    internal sealed class InterceptorEngine
    {
        private readonly IList<Func<object, PropertyInterceptionType, PropertyInfo, object, object>>
            _propertyInterceptors;

        private readonly IList<Func<object, MethodInfo, IDictionary<string, object>, object>> _methodInterceptors;
        private readonly IDictionary<string, PropertyInfo> _properties;
        private readonly IDictionary<string, MethodInfo> _methods;

        internal InterceptorEngine(TypeInfo type)
        {
            _propertyInterceptors = new List<Func<object, PropertyInterceptionType, PropertyInfo, object, object>>();
            _methodInterceptors = new List<Func<object, MethodInfo, IDictionary<string, object>, object>>();
            _properties = type.GetAllProperties().ToDictionary(p => p.Name, p => p);
            _methods = type.GetAbstractAndVirtualMethods().ToDictionary(m => m.GetMethodSignature(), m => m);
        }

        public event EventHandler<InterceptionEventArgs> Intercept;

        public void Add(Func<object, PropertyInterceptionType, PropertyInfo, object, object> interceptor)
        {
            _propertyInterceptors.Add(interceptor);
        }

        public void Add(Func<object, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _methodInterceptors.Add(interceptor);
        }

        public void ClearPropertyInterceptors()
        {
            _propertyInterceptors.Clear();
        }

        public void ClearMethodInterceptors()
        {
            _methodInterceptors.Clear();
        }

        public object InterceptGet(object instance, object value, string propName)
        {
            return InterceptProperty((int) PropertyInterceptionType.GetProperty, instance, value, propName);
        }

        public object InterceptSet(object instance, object value, string propName)
        {
            return InterceptProperty((int) PropertyInterceptionType.SetProperty, instance, value, propName);
        }

        public object InterceptMethod(object instance, IDictionary<string, object> parameters, string methodSignature)
        {
            object nextVal = null;
            var method = _methods[methodSignature];
            var rtype = method.ReturnType.GetTypeInfo();
            var tc = Type.GetTypeCode(rtype);
            object val = null;

            foreach (var interceptor in _methodInterceptors)
            {
                nextVal = interceptor(instance, method, parameters);

                if (!(nextVal is ObjectConstants.Ignore)) // TODO: Not very optimised
                {
                    val = nextVal;
                }
            }

            if (val == null && tc != TypeCode.Object && tc != TypeCode.String)
            {
                val = Activator.CreateInstance(rtype); // return default primative type
            }

            Intercept?.Invoke(this, new InterceptionEventArgs(method, parameters, val));

            return val;
        }

        public object InterceptProperty(int itype, object instance, object value, string propName)
        {
            var val = value;

            var prop = _properties[propName];

            if (_propertyInterceptors.Any())
            {
                foreach (var interceptor in _propertyInterceptors)
                {
                    var nextVal = interceptor(instance, (PropertyInterceptionType) itype, prop, val);

                    if (!(nextVal is ObjectConstants.Ignore)) // TODO: Not very optimised
                    {
                        val = nextVal;
                    }
                }
            }

            Intercept?.Invoke(this, new InterceptionEventArgs(prop, value, val));

            return val;
        }
    }
}