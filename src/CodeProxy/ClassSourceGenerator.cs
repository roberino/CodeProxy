using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeProxy
{
    internal class ClassSourceGenerator<T>
    {
        private static int _typeCounter = 0;

        private readonly TypeInfo _type;

        public ClassSourceGenerator()
        {
            _type = typeof(T).GetTypeInfo();
        }

        public string CreateSource(string asmName, IEnumerable<PropertyInfo> properties, IEnumerable<MethodInfo> methods, IEnumerable<Type> referencedTypes)
        {
            var source = new StringBuilder();
            var name = asmName + "C";
            var targType = typeof(T);
            var targTypeName = GetFullName(targType);
            var interceptorTypeName = "InterceptorEngine<" + targTypeName + ">";

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

            foreach (var prop in properties)
            {
                WritePropDeclaration(prop, source);
            }

            foreach (var method in methods)
            {
                WriteMethods(method, source);
            }

            source.AppendLine("}");

            return source.ToString();
        }

        private string GetAsmName()
        {
            return _type.Name + "I" + (_typeCounter++);
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

        private void WriteMethods(MethodInfo method, StringBuilder output)
        {
            var code = output;
            var type = method.ReturnType;
            var returnTypeName = type.GetTypeName();
            var methodSig = method.GetMethodSignature();
            var tc = Type.GetTypeCode(method.ReturnType);
            var dt = method.DeclaringType.GetTypeInfo();
            var overrideOp = dt.IsClass && ((method.IsAbstract && dt.IsAbstract) || (!method.IsFinal && method.IsVirtual)) ? "override" : null;

            code.Append($"public {overrideOp} {returnTypeName} {method.Name}(");

            int i = 0;

            foreach (var parameter in method.GetParameters())
            {
                code.AppendFormat("{0}{1} {2}", ((i++ > 0) ? "," : ""), parameter.ParameterType.Name, parameter.Name);
            }

            code.AppendLine(") {");
            code.AppendLine("var parameters = new Dictionary<string, object>();");

            foreach (var parameter in method.GetParameters())
            {
                code.AppendFormat("parameters[\"{0}\"] = {1};\n", parameter.Name, parameter.Name);
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
            var returnTypeName = type.GetTypeName();
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
    }
}
