using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class ConsignmentService : IConsignmentService
{
    private readonly IConsignmentRepository _consignmentRepository;
    private readonly ILogger<ConsignmentService> _logger;

    public ConsignmentService(IConsignmentRepository consignmentRepository, ILogger<ConsignmentService> logger)
    {
        _consignmentRepository = consignmentRepository;
        _logger = logger;
    }

    public async Task<(bool success, int consignmentId)> SaveConsignmentAsync(Consignment consignment)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht das Consignment mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}' in der Datenbank zu erstellen.");
            var (success, consignmentId) = await _consignmentRepository.SaveConsignmentAsync(consignment);

            if (success)
            {
                _logger.LogInformation(
                    $"Consignment mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}' wurde erfolgreich in der Datenbank erstellt.");
            }
            else
            {
                _logger.LogError(
                    $"Fehler beim Speichern des Consignment mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}'.");
            }

            return (success, consignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler beim Speichern des Consignment mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}'.");
            throw;
        }
    }

    public async Task<Consignment?> GetConsignmentByIdAsync(int consignmentId)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht das Consignment mit der Id '{consignmentId}' in der Datenbank abzurufen.");
            var consignment = await _consignmentRepository.GetConsignmentByIdAsync(consignmentId);

            _logger.LogInformation(
                $"Consignment mit der Id '{consignmentId}' wurde erfolgreich aus der Datenbank abgerufen.");
            return consignment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Fehler beim abrufen des Consignment mit der Id '{consignmentId}'.");
            throw;
        }
    }

    public async Task UpdateConsignmentAsync(Consignment consignment)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht, das Consignment mit der Id '{consignment.Id}' in der Datenbank zu aktualisieren.");

            await _consignmentRepository.UpdateConsignmentAsync(consignment);

            _logger.LogInformation(
                $"Es wurde erfolgreich das Consignment mit der Id '{consignment.Id}' in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Consignment mit der Id '{consignment.Id}' in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Consignment mit der Id '{consignment.Id}' in der Datenbank.");
            throw new Exception(
                $"Unerwarteter Fehler beim aktualisieren des Consignment mit der Id '{consignment.Id}' in der Datenbank.",
                ex);
        }
    }

    public List<ConsignmentRequest> ParseConsignmentToConsignmentRequest(Consignment consignment)
    {
        var firstEntryType = consignment.Order.Entries.FirstOrDefault()?.DeliveryAddress?.Type;
        if (string.IsNullOrEmpty(firstEntryType))
        {
            _logger.LogError("Type ist null beim konvertieren der Consignment zu einem ConsignmentRequest.");
            throw new Exception("Type ist null beim konvertieren der Consignment zu einem ConsignmentRequest.");
        }

        var firstEntryCountryIsoCode = consignment.Order.Entries.FirstOrDefault()?.DeliveryAddress?.CountryIsoCode;
        if (string.IsNullOrEmpty(firstEntryCountryIsoCode))
        {
            _logger.LogError("CountryIsoCode ist null beim konvertieren der Consignment zu einem ConsignmentRequest.");
            throw new Exception(
                "CountryIsoCode ist null beim konvertieren der Consignment zu einem ConsignmentRequest.");
        }

        var consignmentRequestList = new List<ConsignmentRequest>
        {
            new ConsignmentRequest
            {
                carrier = consignment.Carrier,
                entries = consignment.ConsignmentEntries.Select(e => new ConsignmentEntryRequest
                {
                    orderEntryNumber = e.OrderEntryNumber,
                    quantity = e.Quantity
                }).ToList(),
                shippingAddress = new ConsignmentShippingAddressRequest
                {
                    countryIsoCode = firstEntryCountryIsoCode,
                    type = firstEntryType
                },
                shippingDate = consignment.ShippingDate.ToString("yyyy-MM-dd"),
                status = consignment.Status,
                statusText = consignment.StatusText,
                trackingId = consignment.TrackingId,
                vendorConsignmentCode = consignment.VendorConsignmentCode
            }
        };
        return consignmentRequestList;
    }
}