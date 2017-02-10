using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CodeProxy
{
    internal class AsmGenerator
    {
        private readonly IList<string> _references;

        public AsmGenerator()
        {
            _references = new List<string>();

            UseReference<object>();
        }

        public AsmGenerator UseReference<T>()
        {
            _references.Add(TypeLocation<T>());
            return this;
        }

        public AsmGenerator UseReference(Type refType)
        {
            _references.Add(TypeLocation(refType));
            return this;
        }

        public Assembly Compile(string sourceCode, string assemblyName = null)
        {
            var tree = CreateSyntaxTree(sourceCode);
            var compilation = CreateCompileOptions(tree, assemblyName ?? Path.GetRandomFileName());
            return CreateAsm(compilation);
        }

        private Assembly CreateAsm(CSharpCompilation compilation)
        {
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    IEnumerable<Diagnostic> failures = result.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

                    var sb = new StringBuilder();

                    foreach (Diagnostic diagnostic in failures)
                    {
                        sb.AppendLine(string.Format("{0}: {1}", diagnostic.Id, diagnostic.GetMessage()));
                        if (diagnostic.Location.SourceTree != null) sb.AppendLine(diagnostic.Location.SourceTree.GetText().ToString());
                    }

                    throw new Exception(sb.ToString());
                }
                else
                {
                    ms.Position = 0;

#if NET_STD
                    return System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromStream(ms);
#else
                    return Assembly.Load(ms.ToArray());
#endif
                }
            }
        }

        //private void LoadReferencedAsms()
        //{
        //    var referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
        //    foreach (var referencedAssembly in referencedAssemblies)
        //    {
        //        var loadedAssembly = Assembly.Load(referencedAssembly);

        //        _references.Add(MetadataReference.CreateFromFile(loadedAssembly.Location));
        //    }
        //}

        private SyntaxTree CreateSyntaxTree(string sourceCode)
        {
            return CSharpSyntaxTree.ParseText(sourceCode);
        }

        private CSharpCompilation CreateCompileOptions(SyntaxTree tree, string assemblyName)
        {
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { tree },
                references: _references.Distinct().Select(p => MetadataReference.CreateFromFile(p)).ToList(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private string TypeLocation<T>()
        {
            return TypeLocation(typeof(T));
        }

        private string TypeLocation(Type type)
        {
            return type.GetTypeInfo().Assembly.Location;
        }
    }
}