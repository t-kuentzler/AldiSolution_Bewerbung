using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ConsignmentIsNullException : Exception
{
    public ConsignmentIsNullException()
    {
    }

    protected ConsignmentIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ConsignmentIsNullException(string? message) : base(message)
    {
    }

    public ConsignmentIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}