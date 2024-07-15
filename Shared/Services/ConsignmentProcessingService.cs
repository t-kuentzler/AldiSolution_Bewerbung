using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;

namespace Shared.Services;

public class ConsignmentProcessingService : IConsignmentProcessingService
{
    private readonly IConsignmentService _consignmentService;
    private readonly ICsvFileService _csvFileService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly IValidator<Consignment> _consignmentValidator;
    private readonly IOrderService _orderService;
    private readonly ILogger<ConsignmentProcessingService> _logger;

    public ConsignmentProcessingService(IConsignmentService consignmentService,
        ICsvFileService csvFileService,
        IOAuthClientService oAuthClientService,
        IValidator<Consignment> consignmentValidator,
        IOrderService orderService,
        ILogger<ConsignmentProcessingService> logger)
    {
        _consignmentService = consignmentService;
        _csvFileService = csvFileService;
        _oAuthClientService = oAuthClientService;
        _consignmentValidator = consignmentValidator;
        _orderService = orderService;
        _logger = logger;
    }

    public async Task ReadAndSaveConsignmentsAsync()
    {
        try
        {
            var consignmentsFromCsv = _csvFileService.GetConsignmentsFromCsvFiles();
            var consignments = await _csvFileService.ParseConsignmentsFromCsvToConsignments(consignmentsFromCsv);

            if (consignments.Count != 0)
            {
                foreach (var consignment in consignments)
                {
                    await ProcessSingleConsignmentAsync(consignment);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ein Fehler ist beim auslesen und erzeugen der Lieferungen aufgetreten.");
        }
        finally
        {
            _csvFileService.MoveCsvFilesToArchiv();
        }
    }

    private async Task ProcessSingleConsignmentAsync(Consignment consignment)
    {
        try
        {
            await _consignmentValidator.ValidateAsync(consignment);

            var (success, consignmentId) = await _consignmentService.SaveConsignmentAsync(consignment);

            if (!success)
            {
                return;
            }

            var consignmentRequestList = _consignmentService.ParseConsignmentToConsignmentRequest(consignment);
            var apiResponseList =
                await _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestList, consignment.OrderCode);

            await UpdateConsignmentWithApiResponse(consignmentId, apiResponseList);
            await UpdateOrderStatusIfNeededAsync(consignment.OrderCode, SharedStatus.Shipped);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex,
                $"Validierungsfehler für Consignment. OrderCode: '{consignment.OrderCode}', VendorConsignmentCode: '{consignment.VendorConsignmentCode}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler bei der Verarbeitung des Consignment mit dem OrderCode '{consignment.OrderCode}'.");
        }
    }

    private async Task UpdateConsignmentWithApiResponse(int consignmentId, ConsignmentListResponse? apiResponseList)
    {
        var dbConsignment = await _consignmentService.GetConsignmentByIdAsync(consignmentId);
        if (dbConsignment == null)
        {
            _logger.LogError($"Kein Consignment mit der Id '{consignmentId}' in der Datenbank gefunden.");
            return;
        }

        var aldiConsignmentCode = apiResponseList?.consignments?.FirstOrDefault()?.consignment.aldiConsignmentCode;
        if (string.IsNullOrEmpty(aldiConsignmentCode))
        {
            _logger.LogError(
                $"Der AldiConsignmentCode von der API ist null. Consignment mit der Id '{consignmentId}' konnte nicht aktualisiert werden.");
            return;
        }

        dbConsignment.AldiConsignmentCode = aldiConsignmentCode;
        await _consignmentService.UpdateConsignmentAsync(dbConsignment);
        _logger.LogInformation(
            $"Der ApiConsignmentCode für das Consignment mit der Id '{consignmentId}' wurde erfolgreich aktualisiert.");
    }

    private async Task UpdateOrderStatusIfNeededAsync(string orderCode, string status)
    {
        var orderStatus = await _orderService.GetOrderStatusByOrderCodeAsync(orderCode);
        if (!orderStatus.Equals(status))
        {
            await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);
        }
    }
}