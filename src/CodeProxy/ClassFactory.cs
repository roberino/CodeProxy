using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
            _interceptors = new InterceptorEngine<T>();
            _type = typeof(T).GetTypeInfo();
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
        /// passed the object instance, the method info and the property value</param>
        public ClassFactory<T> AddMethodImplementation(Func<T, MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add((o, m, p) => interceptor((T)o, m, p));
            return this;
        }

        /// <summary>
        /// Adds a method implementation for a specific method
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="interceptor">A function which will be called when a method is invoked - the function will be
        /// passed the object instance, the method info and the property value</param>
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

            var targType = typeof(T);
            var source = new StringBuilder();
            var asmName = GetAsmName();
            var name = asmName + "C";
            var targTypeName = GetFullName(targType);
            var interceptorTypeName = "InterceptorEngine<" + targTypeName + ">";

            var referencedTypes = GetProperties()
                .Select(p => p.PropertyType)
                .Concat(GetMethods().Select(m => m.ReturnType))
                .Concat(GetMethods().SelectMany(m => m.GetParameters().Select(mp => mp.ParameterType))
                .Concat(new Type[] { typeof(Dictionary<string, object>) })
                );

            source.AppendLine("using System;");
            source.AppendLine("using " + targType.Namespace + ";");
            source.AppendLine("using " + GetType().Namespace + ";");
            source.Append(GetUsings(referencedTypes));
            source.AppendLine("public class " + name + " : " + targTypeName + " {");
            source.AppendLine("private readonly Func<int, object, object, string, object> _pi;");
            source.AppendLine("private readonly Func<object, IDictionary<string, object>, string, object> _mi;");
            source.AppendLine("public " + name + "(Func<int, object, object, string, object> pi, Func<object, IDictionary<string, object>, string, object> mi) { _pi = pi; _mi = mi; }");
            source.AppendLine("public T InterceptGet<T>(T val, string name) { return (T)_pi(2, this, val, name); }");
            source.AppendLine("public T InterceptSet<T>(T val, string name) { return (T)_pi(1, this, val, name); }");
            source.AppendLine("public object InterceptMethod(IDictionary<string, object> parameters, string name) { return _mi(this, parameters, name); }");

            foreach (var prop in GetProperties())
            {
                WritePropDeclaration(prop, source);
            }

            foreach (var method in GetMethods())
            {
                WriteMethods(method, source);
            }

            source.AppendLine("}");

            var generator = new AsmGenerator()
                .UseReference<T>()
                .UseReference<AsmGenerator>()
                .UseReference<Dictionary<string, object>>();

            var asm = generator.Compile(source.ToString(), asmName);

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

        private string GetUsings(IEnumerable<Type> types)
        {
            return types.Select(t => t.Namespace).OrderBy(n => n).Distinct().Aggregate(new StringBuilder(), (s, n) => s.AppendLine("using " + n + ";")).ToString();
        }

        private string GetFullName(Type type)
        {
            var nameBuilder = type.Name;

            var cur = type;

            while (cur.DeclaringType != null)
            {
                nameBuilder = cur.DeclaringType.Name + "." + nameBuilder;

                cur = cur.DeclaringType;
            }

            return nameBuilder.ToString();
        }

        private string GetAsmName()
        {
            return _type.Name + "I" + (_typeCounter++);
        }

        private IEnumerable<PropertyInfo> GetProperties()
        {
            return _type.GetProperties().Where(p => (p.GetMethod.IsAbstract || p.GetMethod.IsVirtual) && p.CanRead || p.CanWrite);
        }

        private IEnumerable<MethodInfo> GetMethods()
        {
            return _type.GetMethods().Where(m => !m.IsSpecialName && (m.IsAbstract || m.IsVirtual));
        }

        private void WriteMethods(MethodInfo method, StringBuilder output)
        {
            var code = output;
            var type = method.ReturnType;
            var returnTypeName = GetTypeName(type);
            var methodSig = method.GetMethodSignature();
            var tc = Type.GetTypeCode(method.ReturnType);

            code.Append("public " + returnTypeName + " " + method.Name + "(");

            int i = 0;

            foreach (var parameter in method.GetParameters())
            {
                code.AppendFormat("{0}{1} {2}", ((i++ > 0) ? "," : ""), parameter.ParameterType.Name, parameter.Name);
            }

            code.AppendLine(") {");
            code.AppendLine("var parameters = new Dictionary<string, object>();");

            foreach (var parameter in method.GetParameters())
            {
                code.AppendFormat("parameters[\"{0}\"] = {1};", parameter.Name, parameter.Name);
            }

            code.AppendLine("var res = InterceptMethod(parameters, \"" + methodSig + "\");");

            switch (tc)
            {
                case TypeCode.Object:
                    if (returnTypeName != "void") code.AppendLine("return res as " + returnTypeName + ";");
                    break;
                default:
                    code.AppendLine("return (" + returnTypeName + ")res;");
                    break;
            }

            code.AppendLine("}");
        }

        private void WritePropDeclaration(PropertyInfo prop, StringBuilder output)
        {
            var type = prop.PropertyType;
            var name = prop.Name;
            var returnTypeName = GetTypeName(type);
            var privateName = "_" + GetCamCase(name);

            output.AppendLine("private " + returnTypeName + " " + privateName + ";");
            output.AppendLine("public " + returnTypeName + " " + name +
                " { get { return InterceptGet<" + returnTypeName + ">(" + privateName + ",\"" + name + "\");" + 
                "} set { " + privateName + " = InterceptSet<" + returnTypeName + ">(value,\"" + name + "\"); } }");
        }

        private string GetCamCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            return name.Substring(0, 1).ToLower() + (name.Length > 1 ? name.Substring(1) : "");
        }

        private string GetTypeName(Type type)
        {
            if (type == typeof(void))
            {
                return "void";
            }
            else
            {
                return type.Name;
            }
        }
    }
}