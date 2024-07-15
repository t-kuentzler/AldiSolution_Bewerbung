using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface IReturnService
{
    List<Return> ParseReturnResponseToReturnObject(ReturnResponse returnResponse);
    Task<bool> CreateReturnAsync(Return returnObj);
    ReturnInProgressRequest? ParseReturnToReturnInProgressRequest(Return returnObj);
}