using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class FileSaveException : Exception
{
    public FileSaveException()
    {
    }

    protected FileSaveException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public FileSaveException(string? message) : base(message)
    {
    }

    public FileSaveException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}