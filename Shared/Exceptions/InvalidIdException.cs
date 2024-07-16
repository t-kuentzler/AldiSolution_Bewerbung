using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class InvalidIdException : Exception
{
    public InvalidIdException()
    {
    }

    protected InvalidIdException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public InvalidIdException(string? message) : base(message)
    {
    }

    public InvalidIdException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}