using System;

namespace Overwolf.Application.LogCollector
{
    public class InvalidPathException : Exception
    {
        public InvalidPathException(string message) : base(message) { }
    }
}
