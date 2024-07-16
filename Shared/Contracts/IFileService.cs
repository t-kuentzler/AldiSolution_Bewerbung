using Shared.Entities;

namespace Shared.Contracts;

public interface IFileService
{
    byte[] CreateExcelFileInProgressOrders(List<Order> orders);
    string SaveFileOnServer(byte[] content);
    string GetFilePathByFileId(string fileId);
    MemoryStream GeneratePdf(Return? returnObj);
}