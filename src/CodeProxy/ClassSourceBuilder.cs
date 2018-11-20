using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeProxy
{
    internal class ClassSourceBuilder
    {
        private readonly StringBuilder _source;

        private int _indentCounter;

        public ClassSourceBuilder()
        {
            _source = new StringBuilder();
        }

        public ClassSourceBuilder WriteNamespaceOfType(Type targType)
        {
            return WriteNamespace(targType.Namespace);
        }

        public ClassSourceBuilder WriteNamespaceOfTypes(IEnumerable<Type> types)
        {
            foreach (var type in types) WriteNamespaceOfType(type);

            return this;
        }

        public ClassSourceBuilder WriteNamespace(string namespaceName)
        {
            _source.AppendLine("using " + namespaceName + ";");

            return this;
        }

        public ClassSourceBuilder OpenClassDefinition(string className, params Type[] inherittedTypes)
        {
            var inherittedTypeNames = string.Join(",", inherittedTypes.Select(GetFullName));
            
            _source.AppendLine($"public class {className} : {inherittedTypeNames}" + " {");
            _indentCounter++;

            return this;
        }

        public ClassSourceBuilder CloseBlock()
        {
            _source.AppendLine(string.Empty.PadRight(_indentCounter) + "}");
            _indentCounter--;

            return this;
        }

        public ClassSourceBuilder WriteReadOnlyGetPropertyDefinition(string name, string returnTypeName)
        {
            _source.AppendLine($"public {returnTypeName} {name} {{ get; }}");

            return this;
        }

        public ClassSourceBuilder WritePropertyDefinition(string name, string returnTypeName, string getMethodBody, string setMethodBody)
        {
            _source.AppendLine($"public {returnTypeName} {name} " +
                " { get { " + getMethodBody +
                "} set { " + setMethodBody + "} }");

            return this;
        }

        public ClassSourceBuilder OpenMethod(MethodInfo method, Type inherittedType)
        {
            var returnTypeName = method.ReturnType.GetTypeName();
            var overrideOp = inherittedType.IsClass && ((method.IsAbstract && inherittedType.IsAbstract) || (!method.IsFinal && method.IsVirtual)) ? "override" : null;

            _source.Append($"public {overrideOp} {returnTypeName} {method.Name}(");

            int i = 0;

            foreach (var parameter in method.GetParameters())
            {
                _source.Append($"{((i++ > 0) ? "," : "")}{parameter.ParameterType.Name} {parameter.Name}");
            }

            _source.AppendLine(") {");

            _indentCounter++;

            return this;
        }

        public ClassSourceBuilder AppendLine(string line)
        {
            _source.AppendLine(line);

            return this;
        }

        public ClassSourceBuilder Append(string line)
        {
            _source.Append(line);

            return this;
        }

        public static string GetFullName(Type type)
        {
            var nameBuilder = type.GetTypeName();

            var cur = type;

            while (cur.DeclaringType != null)
            {
                nameBuilder = cur.DeclaringType.Name + "." + nameBuilder;

                cur = cur.DeclaringType;
            }

            return nameBuilder.ToString();
        }

        private void CloseIndents()
        {
            while(_indentCounter > 0)
            {
                CloseBlock();
            }
        }

        public override string ToString()
        {
            CloseIndents();

            return _source.ToString();
        }
    }
}
