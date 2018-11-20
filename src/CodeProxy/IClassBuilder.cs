using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeProxy
{
    public interface IClassBuilder<T, TBuilder> 
        where T : class
        where TBuilder : IClassBuilder<T, TBuilder>
    {
        IClassBuilder<T, TBuilder> AddAsyncMethodImplementation(Func<T, MethodInfo, IDictionary<string, object>, Task> interceptor);
        IClassBuilder<T, TBuilder> AddAsyncMethodImplementation<R>(Func<T, MethodInfo, IDictionary<string, object>, Task<R>> interceptor);
        IClassBuilder<T, TBuilder> AddMethodImplementation(Func<MethodInfo, bool> methodSelector, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor);
        IClassBuilder<T, TBuilder> AddMethodImplementation(Func<MethodInfo, IDictionary<string, object>, object> interceptor);
        IClassBuilder<T, TBuilder> AddMethodImplementation(Func<T, MethodInfo, IDictionary<string, object>, object> interceptor);
        IClassBuilder<T, TBuilder> AddMethodImplementation(string methodName, Func<T, MethodInfo, IDictionary<string, object>, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertyGetter(Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertyGetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertyGetter<O>(Expression<Func<T, O>> propertySelector, Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertyImplementation(Func<PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertySetter(Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertySetter(string propertyName, Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> AddPropertySetter<O>(Expression<Func<T, O>> propertySelector, Func<T, PropertyInfo, object, object> interceptor);
        IClassBuilder<T, TBuilder> ClearAllMethodImplementations();
        IClassBuilder<T, TBuilder> ClearAllPropertyImplementations();
    }
}