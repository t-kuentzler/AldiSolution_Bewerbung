using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IConsignmentService
{
    Task<(bool success, int consignmentId)> SaveConsignmentAsync(Consignment consignment);
    Task<Consignment?> GetConsignmentByIdAsync(int consignmentId);
    Task UpdateConsignmentAsync(Consignment consignment);
    List<ConsignmentRequest> ParseConsignmentToConsignmentRequest(Consignment consignment);
    Task UpdateConsignmentEntryQuantitiesAsync(Order? order, ReturnEntry returnEntry);
    Task<List<Consignment>> GetConsignmentsWithStatusShippedAsync();
    Task<bool> UpdateConsignmentStatusByConsignmentIdAsync(string newStatus, int consignmentId);
    Task<Consignment?> GetConsignmentByConsignmentIdAsync(int consignmentId);
    Task<Consignment?> GetShippedConsignmentByTrackingIdAsync(string trackingId);
    Task<bool> UpdateDpdConsignmentStatusAsync(string newStatus, string trackingId);
    Task<List<Consignment>> SearchConsignmentsAsync(SearchTerm searchTerm, string status);
    Task<List<Consignment>> GetAllConsignmentsByStatusAsync(string status);

}