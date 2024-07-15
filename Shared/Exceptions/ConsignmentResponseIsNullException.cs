using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ConsignmentResponseIsNullException : Exception
{
    public ConsignmentResponseIsNullException()
    {
    }

    protected ConsignmentResponseIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ConsignmentResponseIsNullException(string? message) : base(message)
    {
    }

    public ConsignmentResponseIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}