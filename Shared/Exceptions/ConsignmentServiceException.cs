using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ConsignmentServiceException : Exception
{
    public ConsignmentServiceException()
    {
    }

    protected ConsignmentServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ConsignmentServiceException(string? message) : base(message)
    {
    }

    public ConsignmentServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}