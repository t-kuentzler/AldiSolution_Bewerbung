using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class StatisticServiceException : Exception
{
    public StatisticServiceException()
    {
    }

    protected StatisticServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public StatisticServiceException(string? message) : base(message)
    {
    }

    public StatisticServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}