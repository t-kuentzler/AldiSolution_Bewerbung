using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class QuantityExceededException : Exception
{
    public QuantityExceededException()
    {
    }

    protected QuantityExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public QuantityExceededException(string? message) : base(message)
    {
    }

    public QuantityExceededException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}