using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class UpdateDatabaseException : Exception
{
    public UpdateDatabaseException()
    {
    }

    protected UpdateDatabaseException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public UpdateDatabaseException(string? message) : base(message)
    {
    }

    public UpdateDatabaseException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}