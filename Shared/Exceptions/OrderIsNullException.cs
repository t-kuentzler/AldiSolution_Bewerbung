using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderIsNullException : Exception
{
    public OrderIsNullException()
    {
    }

    protected OrderIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OrderIsNullException(string? message) : base(message)
    {
    }

    public OrderIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}