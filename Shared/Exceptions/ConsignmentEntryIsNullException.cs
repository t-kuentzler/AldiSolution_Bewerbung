using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ConsignmentEntryIsNullException : Exception
{
    public ConsignmentEntryIsNullException()
    {
    }

    protected ConsignmentEntryIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ConsignmentEntryIsNullException(string? message) : base(message)
    {
    }

    public ConsignmentEntryIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}