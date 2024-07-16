using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class CancellationServiceException : Exception
{
    public CancellationServiceException()
    {
    }

    protected CancellationServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public CancellationServiceException(string? message) : base(message)
    {
    }

    public CancellationServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}