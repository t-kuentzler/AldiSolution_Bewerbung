using Shared.Entities;

namespace Shared.Contracts;

public interface IConsignmentRepository
{
    Task<(bool success, int consignmentId)> SaveConsignmentAsync(Consignment consignment);
    Task SaveConsignmentEntryAsync(ConsignmentEntry consignmentEntry);
    Task UpdateConsignmentEntryAsync(ConsignmentEntry consignmentEntry);
    Task<Consignment?> GetConsignmentByIdAsync(int consignmentId);
    Task UpdateConsignmentAsync(Consignment consignment);
    Task<List<Consignment>> GetConsignmentsWithStatusShippedAsync();
}