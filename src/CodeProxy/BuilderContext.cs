using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeProxy
{
    internal sealed class BuilderContext<TInstance> : IBuilderContext where TInstance : class 
    {
        private readonly InterceptorEngine _interceptors;
        private InterceptionEventArgs _capturedArgs;

        internal BuilderContext(ClassFactory<TInstance> factory)
        {
            Instance = factory.CreateInstance();
            var state = ((IHasMutableState) Instance).__state;
            _interceptors = (InterceptorEngine) state;
            _interceptors.Intercept += OnIntercept;
        }

        public TInstance Instance { get; }

        public void AddPropertyImplementation<T>(Func<T> implementation)
        {
            var args = _capturedArgs;
            _capturedArgs = null;

            if (args.Member is MethodInfo)
            {
                _interceptors.Add((i, m, a) =>
                {
                    var args2 = new InterceptionEventArgs(m, a, null);

                    if (IsMatch(args, args2))
                    {
                        return implementation();
                    }

                    return ObjectConstants.IgnoreValue;
                });
            }
            else
            {
                _interceptors.Add((i, t, p, v) =>
                {
                    var args2 = new InterceptionEventArgs(p, v, null);

                    if (IsMatch(args, args2))
                    {
                        return implementation();
                    }

                    return ObjectConstants.IgnoreValue;
                });
            }
        }

        public void AddMemberImplementation<T>(Func<IDictionary<string, object>, T> implementation)
        {
            var args = _capturedArgs;
            _capturedArgs = null;

            if (args.Member is MethodInfo)
            {
                _interceptors.Add((i, m, a) =>
                {
                    var args2 = new InterceptionEventArgs(m, a, null);

                    if (IsMatch(args, args2))
                    {
                        return implementation(a);
                    }

                    return ObjectConstants.IgnoreValue;
                });
            }
            else
            {
                _interceptors.Add((i, t, p, v) =>
                {
                    var args2 = new InterceptionEventArgs(p, v, null);

                    if (IsMatch(args, args2))
                    {
                        return implementation(args2.Parameters);
                    }

                    return ObjectConstants.IgnoreValue;
                });
            }
        }

        private static bool IsMatch(InterceptionEventArgs args1, InterceptionEventArgs args2)
        {
            return args1.Member == args2.Member 
                   && args1.Parameters.Count == args2.Parameters.Count;
        }

        private void OnIntercept(object sender, InterceptionEventArgs e)
        {
            _capturedArgs = e;
        }

        public void Dispose()
        {
            _interceptors.Intercept -= OnIntercept;
        }
    }
}