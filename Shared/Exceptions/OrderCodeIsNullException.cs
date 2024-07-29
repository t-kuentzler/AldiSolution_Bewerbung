using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderCodeIsNullException : Exception
{
    public OrderCodeIsNullException()
    {
    }

    protected OrderCodeIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OrderCodeIsNullException(string? message) : base(message)
    {
    }

    public OrderCodeIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}