using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeProxy
{
    public class ClassFactory<T> where T : class
    {
        private readonly InterceptorEngine<T> _interceptors;
        private readonly TypeInfo _type;

        private static Type _generatedType;
        private static int _typeCounter = 0;

        /// <summary>
        /// Creates a new class factory for creating new classes
        /// </summary>
        public ClassFactory()
        {
            _type = ValidateType();
            _interceptors = new InterceptorEngine<T>();
        }

        public ClassFactory<T> ClearAllPropertyImplementations()
        {
            _interceptors.ClearPropertyInterceptors();
            return this;
        }

        public ClassFactory<T> ClearAllMethodImplementations()
        {
            _interceptors.ClearMethodInterceptors();
            return this;
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the property info and the property value</param>
        public ClassFactory<T> AddPropertyImplementation(Func<PropertyInfo, object, object> interceptor)
        {
            _interceptors.Add((o, d, p, v) => interceptor(p, v));
            return this;
        }

        /// <summary>
        /// Adds a property implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertyGetter(Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, null, interceptor);
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertyGetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {            
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, propertyName, interceptor);
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="propertySelector">An expression which selects the target property</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertyGetter<O>(Expression<Func<T, O>>  propertySelector, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, GetPropertyName(propertySelector), interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertySetter(Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, null, interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertySetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, propertyName, interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="propertySelector">An expression which selects the target property</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public ClassFactory<T> AddPropertySetter<O>(Expression<Func<T, O>> propertySelector, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, GetPropertyName(propertySelector), interceptor);
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public ClassFactory<T> AddMethodImplementation(Func<MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add((o, m, p) => interceptor(m, p));
            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public ClassFactory<T> AddAsyncMethodImplementation<R>(Func<MethodInfo, IDictionary<string, object>, Task<R>> interceptor)
        {
            _interceptors.Add((o, m, p) =>
            {
                if (MethodFilters.AsyncMethods(m))
                {
                    var val = interceptor(m, p);

                    return val;
                }

                return ObjectConstants.IgnoreValue;
            });

            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public ClassFactory<T> AddAsyncMethodImplementation(Func<MethodInfo, IDictionary<string, object>, Task<GenericTaskResult>> interceptor)
        {
            _interceptors.Add((o, m, p) =>
            {
                if (MethodFilters.AsyncMethods(m))
                {
                    var val = interceptor(m, p);

                    return val.Result.ConvertResult();
                }

                return ObjectConstants.IgnoreValue;
            });

            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the parameters as a dictionary</param>
        public ClassFactory<T> AddMethodImplementation(Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add((o, m, p) => interceptor((T)o, m, p));
            return this;
        }

        /// <summary>
        /// Adds a method implementation for a specific method
        /// </summary>
        /// <param name="methodSelector">A predicate which selects relevant methods</param>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the parameters as a dictionary</param>
        public ClassFactory<T> AddMethodImplementation(Func<MethodInfo, bool> methodSelector, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add((o, m, p) =>
            {
                if ((methodSelector?.Invoke(m)).GetValueOrDefault())
                {
                    return interceptor((T)o, m, p);
                }
                return ObjectConstants.IgnoreValue;
            });
            return this;
        }

        /// <summary>
        /// Adds a method implementation for a specific method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the parameters as a dictionary</param>
        public ClassFactory<T> AddMethodImplementation(string methodName, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add((o, m, p) =>
            {
                if (string.Equals(m.Name, methodName))
                {
                    return interceptor((T)o, m, p);
                }
                return ObjectConstants.IgnoreValue;
            });
            return this;
        }
        
        /// <summary>
        /// Generates the type implementation
        /// </summary>
        /// <returns></returns>
        public Type CreateType()
        {
            var gt = _generatedType;

            if (gt != null) return gt;

            var asmName = GetAsmName();
            var properties = GetProperties();
            var methods = GetMethods();

            var referencedTypes = properties
                .Select(p => p.PropertyType)
                .Concat(methods.Select(m => m.ReturnType))
                .Concat(methods.SelectMany(m => m.GetParameters().Select(mp => mp.ParameterType))
                .Concat(GetBaseTypes())
                .Concat(new Type[] { typeof(Dictionary<string, object>) })
                );

            var source = new ClassSourceGenerator<T>().CreateSource(asmName, properties, methods, referencedTypes);

            var generator = new AsmGenerator()
                .UseReference<T>()
                .UseReference<AsmGenerator>()
                .UseReference<Dictionary<string, object>>();

            foreach(var rtype in referencedTypes)
            {
                generator.UseReference(rtype);
            }

            SourceCreated?.Invoke(source);

            File.WriteAllText("prox.cs", source);

            var asm = generator.Compile(source, asmName);

            var type = asm.ExportedTypes.First();

            _generatedType = type;

            return type;
        }

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        public T CreateInstance()
        {
            return (T)Activator.CreateInstance(CreateType(), new Func<int, object, object, string, object>(_interceptors.InterceptProperty), new Func<object, IDictionary<string, object>, string, object>(_interceptors.InterceptMethod));
        }

        internal event Action<string> SourceCreated;

        internal static string GetPropertyName<TField>(Expression<Func<T, TField>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression).Member.Name;
        }

        private TypeInfo ValidateType()
        {
            var type = typeof(T).GetTypeInfo();

            if (type.IsSealed)
            {
                throw new ArgumentException($"Sealed types not supported: {type.FullName}");
            }

            if (type.IsGenericTypeDefinition)
            {
                throw new ArgumentException($"Open generic types not supported: {type.FullName}");
            }

            return type;
        }

        private ClassFactory<T> AddPropertyImplementation(PropertyInterceptionType interceptType, string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {
            _interceptors.Add((o, d, p, v) =>
            {
                if (d == interceptType && (propertyName == null || string.Equals(p.Name, propertyName)))
                {
                    return interceptor((T)o, p, v);
                }
                return ObjectConstants.IgnoreValue;
            });

            return this;
        }

        private string GetAsmName()
        {
            return _type.Name + "I" + (_typeCounter++);
        }

        private IEnumerable<Type> GetBaseTypes()
        {
            var t = _type.BaseType;

            while (t != typeof(object) && t != null)
            {
                yield return t;

                t = t.GetTypeInfo().BaseType;
            }

            var ift = _type.GetInterfaces().ToList();

            while (ift.Any())
            {
                var iftn = new List<Type>();

                foreach (var t2 in ift)
                {
                    iftn.AddRange(t2.GetTypeInfo().GetInterfaces());
                    yield return t2;
                }

                ift = iftn;
            }
        }

        private IEnumerable<PropertyInfo> GetProperties()
        {
            return _type
                .GetAllProperties()
                .Where(p => (p.GetMethod.IsAbstract || p.GetMethod.IsVirtual) && p.CanRead || p.CanWrite);
        }

        private IEnumerable<MethodInfo> GetMethods()
        {
            return _type.GetAbstractAndVirtualMethods();
        }

        private IEnumerable<TypeInfo> GetTypeChain(TypeInfo type)
        {
            yield return type;

            if (type.BaseType != null)
            {
                foreach (var ifaceb in GetTypeChain(type.BaseType.GetTypeInfo()))
                {
                    yield return ifaceb;
                }
            }

            foreach(var iface in type.GetInterfaces())
            {
                var ifacet = iface.GetTypeInfo();

                yield return ifacet;

                foreach(var ifaceb in GetTypeChain(ifacet))
                {
                    yield return ifaceb;
                }
            }
        }
    }
}