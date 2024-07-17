using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnConsignmentAndPackageServiceException : Exception
{
    public ReturnConsignmentAndPackageServiceException()
    {
    }

    protected ReturnConsignmentAndPackageServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnConsignmentAndPackageServiceException(string? message) : base(message)
    {
    }

    public ReturnConsignmentAndPackageServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}