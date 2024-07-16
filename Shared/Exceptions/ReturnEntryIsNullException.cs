using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnEntryIsNullException : Exception
{
    public ReturnEntryIsNullException()
    {
    }

    protected ReturnEntryIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnEntryIsNullException(string? message) : base(message)
    {
    }

    public ReturnEntryIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}