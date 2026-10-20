using System;

namespace Lodino.Core.Errors
{
    public class LodinoException : Exception
    {
        public LodinoException(string message) : base(message) { }
        public LodinoException(string message, Exception inner) : base(message, inner) { }
    }

    public class CorruptDataException : LodinoException
    {
        public CorruptDataException(string message) : base(message) { }
        public CorruptDataException(string message, Exception inner) : base(message, inner) { }
    }

    public class ResourceLimitException : LodinoException
    {
        public ResourceLimitException(string message) : base(message) { }
    }

    public class UnsupportedFormatException : LodinoException
    {
        public UnsupportedFormatException(string message) : base(message) { }
    }

    public class PathSecurityException : LodinoException
    {
        public PathSecurityException(string message) : base(message) { }
    }
}
