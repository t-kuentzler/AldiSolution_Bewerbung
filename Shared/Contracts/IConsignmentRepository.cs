using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IConsignmentRepository
{
    Task<(bool success, int consignmentId)> SaveConsignmentAsync(Consignment consignment);
    Task SaveConsignmentEntryAsync(ConsignmentEntry consignmentEntry);
    Task UpdateConsignmentEntryAsync(ConsignmentEntry consignmentEntry);
    Task<Consignment?> GetConsignmentByIdAsync(int consignmentId);
    Task UpdateConsignmentAsync(Consignment consignment);
    Task<List<Consignment>> GetConsignmentsWithStatusShippedAsync();
    Task<bool> UpdateConsignmentStatusByIdAsync(int consignmentId, string newStatus);
    Task<Consignment?> GetConsignmentByConsignmentIdAsync(int consignmentId);
    Task<Consignment?> GetShippedConsignmentByTrackingIdAsync(string trackingId);
    Task UpdateConsignmentStatusByTrackingIdAsync(string newStatus, string trackingId);
    Task<List<Consignment>> SearchShippedConsignmentsAsync(SearchTerm searchTerm);
    Task<List<Consignment>> GetConsignmentsWithStatusAsync(string status);
}