using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderEntryIsNullException : Exception
{
    public OrderEntryIsNullException()
    {
    }

    protected OrderEntryIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public OrderEntryIsNullException(string? message) : base(message)
    {
    }

    public OrderEntryIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}