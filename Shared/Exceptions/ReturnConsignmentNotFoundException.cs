using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnConsignmentNotFoundException : Exception
{
    public ReturnConsignmentNotFoundException()
    {
    }

    protected ReturnConsignmentNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnConsignmentNotFoundException(string? message) : base(message)
    {
    }

    public ReturnConsignmentNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}