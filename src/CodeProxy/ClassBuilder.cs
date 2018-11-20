using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeProxy
{
    public class ClassBuilder<T, TBuilder> : IClassBuilder<T, TBuilder> 
        where T:class 
        where TBuilder : IClassBuilder<T, TBuilder>
    {
        /// <summary>
        /// Creates a new class factory for creating new classes
        /// </summary>
        internal ClassBuilder(InterceptorEngine interceptors)
        {
            Interceptors = interceptors;
        }

        internal InterceptorEngine Interceptors { get; }

        public IClassBuilder<T, TBuilder> ClearAllPropertyImplementations()
        {
            Interceptors.ClearPropertyInterceptors();
            return this;
        }

        public IClassBuilder<T, TBuilder> ClearAllMethodImplementations()
        {
            Interceptors.ClearMethodInterceptors();
            return this;
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertyImplementation(Func<PropertyInfo, object, object> interceptor)
        {
            Interceptors.Add((o, d, p, v) => interceptor(p, v));
            return this;
        }

        /// <summary>
        /// Adds a property implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertyGetter(Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, null, interceptor);
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertyGetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {            
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, propertyName, interceptor);
        }

        /// <summary>
        /// Adds a property get implementation
        /// </summary>
        /// <param name="propertySelector">An expression which selects the target property</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertyGetter<O>(Expression<Func<T, O>>  propertySelector, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.GetProperty, GetPropertyName(propertySelector), interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertySetter(Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, null, interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertySetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, propertyName, interceptor);
        }

        /// <summary>
        /// Adds a property set implementation
        /// </summary>
        /// <param name="propertySelector">An expression which selects the target property</param>
        /// <param name="interceptor">A function which will be called when a property is invoked - the function will be
        /// passed the object instance, the property info and the property value</param>
        public IClassBuilder<T, TBuilder> AddPropertySetter<O>(Expression<Func<T, O>> propertySelector, Func<T, PropertyInfo, object, object> interceptor)
        {
            return AddPropertyImplementation(PropertyInterceptionType.SetProperty, GetPropertyName(propertySelector), interceptor);
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public IClassBuilder<T, TBuilder> AddMethodImplementation(Func<MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            Interceptors.Add((o, m, p) => interceptor(m, p));
            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public IClassBuilder<T, TBuilder> AddAsyncMethodImplementation<R>(Func<T, MethodInfo, IDictionary<string, object>, Task<R>> interceptor)
        {
            Interceptors.Add((o, m, p) =>
            {
                if (!MethodFilters.AsyncMethods(m)) return ObjectConstants.IgnoreValue;
                
                var val = interceptor((T)o, m, p);

                return val;

            });

            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the method info and the property value</param>
        public IClassBuilder<T, TBuilder> AddAsyncMethodImplementation(
            Func<T, MethodInfo, IDictionary<string, object>, Task> interceptor)
        {
            Interceptors.Add((o, m, p) =>
            {
                if (!MethodFilters.AsyncMethods(m)) return ObjectConstants.IgnoreValue;
                
                var val = interceptor((T) o, m, p);

                if (!m.ReturnType.IsGenericType) return val;
                
                var taskType = m.ReturnType.GetGenericArguments().FirstOrDefault();

                var genTask = new GenericTaskResult(val, val.GetTaskResult(), taskType);

                return genTask.ConvertResult();

            });

            return this;
        }

        /// <summary>
        /// Adds a method implementation
        /// </summary>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the parameters as a dictionary</param>
        public IClassBuilder<T, TBuilder> AddMethodImplementation(Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            Interceptors.Add((o, m, p) => MethodFilters.AsyncMethods(m) ? interceptor((T) o, m, p) : ObjectConstants.IgnoreValue);
            return this;
        }

        /// <summary>
        /// Adds a method implementation for a specific method
        /// </summary>
        /// <param name="methodSelector">A predicate which selects relevant methods</param>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the parameters as a dictionary</param>
        public IClassBuilder<T, TBuilder> AddMethodImplementation(Func<MethodInfo, bool> methodSelector, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            Interceptors.Add((o, m, p) =>
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
        public IClassBuilder<T, TBuilder> AddMethodImplementation(string methodName, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            Interceptors.Add((o, m, p) =>
            {
                if (string.Equals(m.Name, methodName))
                {
                    return interceptor((T)o, m, p);
                }
                return ObjectConstants.IgnoreValue;
            });
            return this;
        }

        private IClassBuilder<T, TBuilder> AddPropertyImplementation(PropertyInterceptionType interceptType, string propertyName, Func<T, PropertyInfo, object, object> interceptor)
        {
            Interceptors.Add((o, d, p, v) =>
            {
                if (d == interceptType && (propertyName == null || string.Equals(p.Name, propertyName)))
                {
                    return interceptor((T)o, p, v);
                }
                return ObjectConstants.IgnoreValue;
            });

            return this;
        }

        internal static string GetPropertyName<TField>(Expression<Func<T, TField>> propertyExpression)
        {
            return (propertyExpression.Body as MemberExpression ?? ((UnaryExpression)propertyExpression.Body).Operand as MemberExpression).Member.Name;
        }
    }
}
