using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class OrderServiceException : Exception
{
    public OrderServiceException()
    {
    }
    
    public OrderServiceException(string? message) : base(message)
    {
    }

    public OrderServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}