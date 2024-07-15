using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException()
    {
    }

    protected OrderNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OrderNotFoundException(string? message) : base(message)
    {
    }

    public OrderNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}