namespace Shared.Contracts;

public interface IQuantityCheckService
{
    bool IsQuantityExceedingAvailable(int existingQuantity, int adjustmentQuantity, int totalQuantity);
}