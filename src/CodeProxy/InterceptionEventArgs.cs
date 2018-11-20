using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeProxy
{
    internal class InterceptionEventArgs : EventArgs
    {
        public InterceptionEventArgs(MemberInfo member, IDictionary<string, object> parameters, object returnValue)
        {
            Member = member;
            Parameters = parameters;
            ReturnValue = returnValue;
        }

        public InterceptionEventArgs(MemberInfo member, object parameter, object returnValue = null)
        {
            Member = member;
            Parameters = new Dictionary<string, object>() {["$value"] = parameter};
            ReturnValue = returnValue;
        }

        public MemberInfo Member { get; }

        public IDictionary<string, object> Parameters { get; }

        public object ReturnValue { get; }
    }
}