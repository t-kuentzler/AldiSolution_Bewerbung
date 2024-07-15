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

}