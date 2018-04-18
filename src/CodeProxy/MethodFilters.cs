using System;
using System.Reflection;
using System.Threading.Tasks;

namespace CodeProxy
{
    public static class MethodFilters
    {
        private static readonly TypeInfo _taskType = typeof(Task).GetTypeInfo();

        public static bool AsyncMethods(MethodInfo m)
        {
            return _taskType.IsAssignableFrom(m.ReturnType);
        }

        public static bool NonAsyncMethods(MethodInfo m)
        {
            return !_taskType.IsAssignableFrom(m.ReturnType);
        }

        public static bool PrimativeMethods(MethodInfo m)
        {
            var tc = Type.GetTypeCode(m.ReturnType);

            return tc != TypeCode.Object
                && tc != TypeCode.String
                && tc != TypeCode.Empty;
        }

        public static bool NonPrimativeMethods(MethodInfo m)
        {
            var tc = Type.GetTypeCode(m.ReturnType);

            return tc == TypeCode.Object
                || tc == TypeCode.String;
        }

        public static bool VoidMethods(MethodInfo m)
        {
            return m.ReturnType == typeof(void);
        }
    }
}