using Shared.Contracts;

namespace Shared.Services;

public class QuantityCheckService : IQuantityCheckService
{
    //Prüfung, ob vorhandene menge und neu hinzufügende Menge die gesamtmenge überschreiten (Für Stornierung/Retouren)
    public bool IsQuantityExceedingAvailable(int existingQuantity, int adjustmentQuantity, int totalQuantity)
    {
        return (existingQuantity + adjustmentQuantity) > totalQuantity;
    }
}