using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnService
{
    List<Return> ParseReturnResponseToReturnObject(ReturnResponse returnResponse);
    Task<bool> CreateReturnAsync(Return returnObj);
    ReturnInProgressRequest? ParseReturnToReturnInProgressRequest(Return returnObj);

    Task<ReturnProcessingResult> ProcessReturn(int orderId,
        Dictionary<int, ReturnEntryModel> returnEntries);

    Task ProcessManualReturnAsync(int orderId, ManualReturnResponse manualReturnResponse,
        Dictionary<int, ReturnEntryModel> returnEntries);

    Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, List<string> statuses);
}