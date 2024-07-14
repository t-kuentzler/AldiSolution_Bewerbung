using System;

namespace Shared.Exceptions
{
    public class CreateOrderException : Exception
    {
        public CreateOrderException(string? orderCode, Exception ex) : base($"Fehler beim Erstellen des Datensatzes für Order mit dem OrderCode {orderCode}: {ex.Message}.")
        {
        }
    }
}
