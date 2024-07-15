using Shared.Entities;
using Shared.Models;

namespace Shared.Contracts;

public interface ICsvFileService
{
    List<ConsignmentFromCsv> GetConsignmentsFromCsvFiles();
    Task<List<Consignment>> ParseConsignmentsFromCsvToConsignments(List<ConsignmentFromCsv> csvConsignments);
    void MoveCsvFilesToArchiv();
}