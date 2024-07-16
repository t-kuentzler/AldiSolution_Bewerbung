using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class DeliveryAddressIsNullException : Exception
{
    public DeliveryAddressIsNullException()
    {
    }

    protected DeliveryAddressIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public DeliveryAddressIsNullException(string? message) : base(message)
    {
    }

    public DeliveryAddressIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}