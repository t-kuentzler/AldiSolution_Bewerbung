using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnIsNullException : Exception
{
    public ReturnIsNullException()
    {
    }

    protected ReturnIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnIsNullException(string? message) : base(message)
    {
    }

    public ReturnIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}