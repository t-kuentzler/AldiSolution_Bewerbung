using System.Runtime.Serialization;

namespace Shared.Exceptions;

public class PdfGenerationException : Exception
{
    public PdfGenerationException()
    {
    }

    protected PdfGenerationException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public PdfGenerationException(string? message) : base(message)
    {
    }

    public PdfGenerationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}