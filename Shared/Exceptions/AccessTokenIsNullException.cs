using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class AccessTokenIsNullException : Exception
{
    public AccessTokenIsNullException()
    {
    }

    protected AccessTokenIsNullException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public AccessTokenIsNullException(string? message) : base(message)
    {
    }

    public AccessTokenIsNullException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}