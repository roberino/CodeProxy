using System;
using System.Collections.Generic;
using System.Reflection;

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
            var source = new ClassSourceBuilder();

            var name = asmName + "C";
            var targType = typeof(T);
            var targTypeName = ClassSourceBuilder.GetFullName(targType);
            var interceptorTypeName = "InterceptorEngine<" + targTypeName + ">";

            source
                .WriteNamespace("System")
                .WriteNamespaceOfType(targType)
                .WriteNamespaceOfType(GetType())
                .WriteNamespaceOfTypes(referencedTypes)
                .OpenClassDefinition(name, targType, typeof(IHasMutableState));

            source.AppendLine("private readonly Func<int, object, object, string, object> _pi;");
            source.AppendLine("private readonly Func<object, IDictionary<string, object>, string, object> _mi;");
            source.AppendLine("public " + name + "(Func<int, object, object, string, object> pi, Func<object, IDictionary<string, object>, string, object> mi, object state) { _pi = pi; _mi = mi; __state = state; }");
            source.AppendLine("public T InterceptGet<T>(T val, string name) { return (T)_pi(2, this, val, name); }");
            source.AppendLine("public T InterceptSet<T>(T val, string name) { return (T)_pi(1, this, val, name); }");
            source.AppendLine("public object InterceptMethod(IDictionary<string, object> parameters, string name) { return _mi(this, parameters, name); }");

            source.WriteReadOnlyGetPropertyDefinition("__state", "object");

            foreach (var prop in properties)
            {
                WritePropDeclaration(prop, source);
            }

            foreach (var method in methods)
            {
                WriteMethods(method, source);
            }

            source.CloseBlock();

            return source.ToString();
        }

        private string GetAsmName()
        {
            return _type.Name + "I" + (_typeCounter++);
        }

        private void WriteMethods(MethodInfo method, ClassSourceBuilder output)
        {
            var code = output;
            var type = method.ReturnType;
            var returnTypeName = type.GetTypeName();
            var methodSig = method.GetMethodSignature();
            var tc = Type.GetTypeCode(method.ReturnType);
            var dt = method.DeclaringType.GetTypeInfo();

            code.OpenMethod(method, dt);

            code.AppendLine("var parameters = new Dictionary<string, object>();");

            foreach (var parameter in method.GetParameters())
            {
                code.AppendLine($"parameters[\"{parameter.Name}\"] = {parameter.Name};");
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

            code.CloseBlock();
        }

        private void WritePropDeclaration(PropertyInfo prop, ClassSourceBuilder output)
        {
            var type = prop.PropertyType;
            var name = prop.Name;
            var returnTypeName = type.GetTypeName();
            var privateName = "_" + GetCamCase(name);

            output.AppendLine("private " + returnTypeName + " " + privateName + ";");

            var gb = " return InterceptGet<" + returnTypeName + ">(" + privateName + ",\"" + name + "\");";
            var sb = privateName + " = InterceptSet<" + returnTypeName + ">(value,\"" + name + "\");";

            output.WritePropertyDefinition(name, returnTypeName, gb, sb);
        }

        private string GetCamCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            return name.Substring(0, 1).ToLower() + (name.Length > 1 ? name.Substring(1) : "");
        }
    }
}
