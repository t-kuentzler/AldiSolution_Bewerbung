using System;

namespace Shared.Exceptions
{
    public class UpdateOrderStatusException : Exception
    {
        public string OrderCode { get; }

        public UpdateOrderStatusException(string orderCode, Exception ex)
            : base($"Fehler beim Aktualisieren des Status IN_PROGRESS für Bestellung mit Code {orderCode}. Error: {ex.Message}", ex)
        {
            OrderCode = orderCode;
        }
    }
}
