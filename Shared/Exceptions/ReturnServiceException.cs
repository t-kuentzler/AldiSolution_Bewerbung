using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnServiceException : Exception
{
    public ReturnServiceException()
    {
    }

    protected ReturnServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnServiceException(string? message) : base(message)
    {
    }

    public ReturnServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}