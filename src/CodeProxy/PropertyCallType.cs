using System;

namespace CodeProxy
{
    [Flags]
    public enum PropertyCallType : byte
    {
        None = 0,
        Get = 1,
        Set = 2
    }
}
