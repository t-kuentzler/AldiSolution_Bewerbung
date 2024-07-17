using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class ReturnConsignmentAndPackageServiceArgumentException : Exception
{
    public ReturnConsignmentAndPackageServiceArgumentException()
    {
    }

    protected ReturnConsignmentAndPackageServiceArgumentException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public ReturnConsignmentAndPackageServiceArgumentException(string? message) : base(message)
    {
    }

    public ReturnConsignmentAndPackageServiceArgumentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}