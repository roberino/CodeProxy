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

        private bool _isDirty;
        private static Type _generatedType;
        private static int _typeCounter = 0;

        public ClassFactory()
        {
            _isDirty = true;
            _interceptors = new InterceptorEngine<T>();
        }

        public ClassFactory<T> AddPropertyImplementation(Func<PropertyInfo, object, object> interceptor)
        {
            _interceptors.Add(interceptor);
            _isDirty = true;
            return this;
        }

        public ClassFactory<T> AddMethodImplementation(Func<MethodInfo, IDictionary<string, object>, object> interceptor)
        {
            _interceptors.Add(interceptor);
            _isDirty = true;
            return this;
        }

        public Type CreateType()
        {
            var gt = _generatedType;

            if (!_isDirty && gt != null) return gt;

            var targType = typeof(T);
            var source = new StringBuilder();
            var asmName = GetAsmName();
            var name = asmName + "C";
            var targTypeName = GetFullName(targType);
            var interceptorTypeName = "InterceptorEngine<" + targTypeName + ">";

            var referencedTypes = GetProperties<T>()
                .Select(p => p.PropertyType)
                .Concat(GetMethods<T>().Select(m => m.ReturnType))
                .Concat(GetMethods<T>().SelectMany(m => m.GetParameters().Select(mp => mp.ParameterType))
                .Concat(new Type[] { typeof(Dictionary<string, object>) })
                );

            source.AppendLine("using System;");
            source.AppendLine("using " + targType.Namespace + ";");
            source.AppendLine("using " + GetType().Namespace + ";");
            source.Append(GetUsings(referencedTypes));
            source.AppendLine("public class " + name + " : " + targTypeName + " {");
            source.AppendLine("private readonly Func<object, string, object> _pi;");
            source.AppendLine("private readonly Func<IDictionary<string, object>, string, object> _mi;");
            source.AppendLine("public " + name + "(Func<object, string, object> pi, Func<IDictionary<string, object>, string, object> mi) { _pi = pi; _mi = mi; }");
            source.AppendLine("public T InterceptGet<T>(T val, string name) { return (T)_pi(val, name); }");
            source.AppendLine("public object InterceptMethod(IDictionary<string, object> parameters, string name) { return _mi(parameters, name); }");

            foreach (var prop in GetProperties<T>())
            {
                WritePropDeclaration(prop, source);
            }

            foreach (var method in GetMethods<T>())
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

        public T CreateInstance()
        {
            return (T)Activator.CreateInstance(CreateType(), new Func<object, string, object>(_interceptors.InterceptGet), new Func<IDictionary<string, object>, string, object>(_interceptors.InterceptMethod));
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
            return typeof(T).Name + "I" + (_typeCounter++);
        }

        private IEnumerable<PropertyInfo> GetProperties<T>()
        {
            return typeof(T).GetTypeInfo().GetProperties().Where(p => (p.GetMethod.IsAbstract || p.GetMethod.IsVirtual) && p.CanRead || p.CanWrite);
        }

        private IEnumerable<MethodInfo> GetMethods<T>()
        {
            return typeof(T).GetTypeInfo().GetMethods().Where(m => !m.IsSpecialName && (m.IsAbstract || m.IsVirtual));
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
                " { get { return InterceptGet<" + returnTypeName + ">(" + privateName + ",\"" + name +
                "\"); } set { " + privateName + " = value; } }");
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