using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderStatusIsNullException : Exception
{
    public OrderStatusIsNullException()
    {
    }

    protected OrderStatusIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OrderStatusIsNullException(string? message) : base(message)
    {
    }

    public OrderStatusIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}