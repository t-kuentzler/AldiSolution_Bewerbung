namespace Shared.Exceptions;

public class AccessTokenServiceException : Exception
{
    public AccessTokenServiceException()
    {
    }

    public AccessTokenServiceException(string? message) : base(message)
    {
    }

    public AccessTokenServiceException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}