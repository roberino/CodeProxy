using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeProxy
{
    public class ClassFactory<T> : ClassBuilder<T, ClassFactory<T>>
        where T : class
    {
        private readonly TypeInfo _type;

        private static Type _generatedType;
        private static int _typeCounter = 0;

        /// <summary>
        /// Creates a new class factory for creating new classes
        /// </summary>
        public ClassFactory() : base(new InterceptorEngine(typeof(T).GetTypeInfo()))
        {
            _type = ValidateType();
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
                ).ToArray();

            var source = new ClassSourceGenerator<T>().CreateSource(asmName, properties, methods, referencedTypes);

            var generator = new AsmGenerator()
                .UseReference<T>()
                .UseReference<AsmGenerator>()
                .UseReference<Dictionary<string, object>>();

            foreach(var rtype in referencedTypes)
            {
                generator.UseReference(rtype);
            }

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
            return (T)Activator.CreateInstance(CreateType(), 
                new Func<int, object, object, string, object>(Interceptors.InterceptProperty), 
                new Func<object, IDictionary<string, object>, string, object>(Interceptors.InterceptMethod), 
                Interceptors);
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

        private string GetAsmName()
        {
            return _type.GetSanitisedTypeName() + "I" + (_typeCounter++);
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

        private IReadOnlyCollection<PropertyInfo> GetProperties()
        {
            return _type
                .GetAllProperties()
                .Where(p => (p.GetMethod.IsAbstract || p.GetMethod.IsVirtual) && p.CanRead || p.CanWrite)
                .ToArray();
        }

        private IReadOnlyCollection<MethodInfo> GetMethods()
        {
            return _type.GetAbstractAndVirtualMethods().ToArray();
        }
    }
}