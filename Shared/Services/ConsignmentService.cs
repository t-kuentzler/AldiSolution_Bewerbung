using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class ConsignmentService : IConsignmentService
{
    private readonly IConsignmentRepository _consignmentRepository;
    private readonly ILogger<ConsignmentService> _logger;
    private readonly IValidatorWrapper<SearchTerm> _searchTermValidator;


    public ConsignmentService(IConsignmentRepository consignmentRepository, ILogger<ConsignmentService> logger,
        IValidatorWrapper<SearchTerm> searchTermValidator)
    {
        _consignmentRepository = consignmentRepository;
        _logger = logger;
        _searchTermValidator = searchTermValidator;
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

    public async Task UpdateConsignmentEntryQuantitiesAsync(Order? order, ReturnEntry returnEntry)
    {
        if (order == null)
        {
            _logger.LogError($"'{nameof(order)}' darf nicht null sein.");
            throw new ArgumentNullException(nameof(order));
        }

        if (returnEntry == null)
        {
            _logger.LogError($"'{nameof(returnEntry)}' darf nicht null sein.");
            throw new ArgumentNullException(nameof(returnEntry));
        }

        int remainingQuantityToReturn = returnEntry.Quantity;

        foreach (var consignment in order.Consignments)
        {
            var consignmentEntry = consignment.ConsignmentEntries
                .FirstOrDefault(ce => ce.OrderEntryNumber == returnEntry.OrderEntryNumber);

            if (consignmentEntry != null)
            {
                int quantityToReturnFromEntry = Math.Min(remainingQuantityToReturn,
                    consignmentEntry.Quantity - consignmentEntry.CancelledOrReturnedQuantity);
                if (quantityToReturnFromEntry > 0)
                {
                    await UpdateConsignmentEntryQuantity(consignmentEntry, quantityToReturnFromEntry);
                    remainingQuantityToReturn -= quantityToReturnFromEntry;
                }

                if (remainingQuantityToReturn <= 0)
                {
                    break;
                }
            }
        }
    }

    private async Task UpdateConsignmentEntryQuantity(ConsignmentEntry entry, int quantityToReturn)
    {
        if (entry == null)
        {
            _logger.LogError($"'{nameof(entry)}' darf nicht null sein.");
            throw new ArgumentNullException(nameof(entry));
        }

        if (quantityToReturn <= 0)
        {
            _logger.LogError("Die Quantity muss positiv sein.");
            throw new ArgumentException("Die Quantity muss positiv sein.", nameof(quantityToReturn));
        }

        entry.CancelledOrReturnedQuantity += quantityToReturn;

        try
        {
            _logger.LogInformation(
                $"Es wird versucht, die Quantity des Consignment mit der Id '{entry.ConsignmentId}' in der Datenbank zu aktualisieren.");
            await _consignmentRepository.UpdateConsignmentEntryAsync(entry);
            _logger.LogInformation(
                $"Es wurde erfolgreich die Quantity des Consignment mit der Id '{entry.ConsignmentId}' in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{entry.Id}' in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{entry.Id}' in der Datenbank.");
            throw new Exception(
                $"Unerwarteter Fehler beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{entry.Id}' in der Datenbank.",
                ex);
        }
    }
    
    public async Task<List<Consignment>> GetConsignmentsWithStatusShippedAsync()
    {
        try
        {
            var consignments = await _consignmentRepository.GetConsignmentsWithStatusShippedAsync();

            return consignments;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim abrufen der Consignments mit dem Status '{SharedStatus.Shipped}'.");
            return new List<Consignment>();

        }
        catch (GetConsignmentsWithStatusShippedException ex)
        {
            _logger.LogError(
                ex, $"Es ist ein unerwarteter Fehler beim abrufen aller Consignment mit dem Status SHIPPED aufgetreten.");
            return new List<Consignment>();
        }
    }
    
    public async Task<bool> UpdateConsignmentStatusByConsignmentIdAsync(string newStatus, int consignmentId)
    {
        try
        {
            string status;

            if (newStatus.Equals(SharedStatus.delivered))
            {
                status = SharedStatus.Delivered;
            }
            else
            {
                return false;
            }

            await _consignmentRepository.UpdateConsignmentStatusByIdAsync(consignmentId, status);
            
            _logger.LogInformation($"Der Status der Lieferung mit der ConsignmentId '{consignmentId}' wurde erfolgreich auf '{status}' aktualisiert.");
            return true;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Consignment mit der ConsignmentId '{consignmentId}'.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler beim Aktualisieren des Status für das Consignment mit der ConsignmentId '{consignmentId}'.");
            return false;
        }
    }
    
    public async Task<Consignment?> GetConsignmentByConsignmentIdAsync(int consignmentId)
    {
        try
        {
            var consignment = await _consignmentRepository.GetConsignmentByConsignmentIdAsync(consignmentId);

            return consignment;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim abrufen des Consignment mit der ConsignmentId '{consignmentId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Es ist ein unerwarteter Fehler beim abrufen des Consignment mit der Consignment Id '{consignmentId}' aufgetreten.");
            throw;
        }
    }
    
    public async Task<Consignment?> GetShippedConsignmentByTrackingIdAsync(string trackingId)
    {
        try
        {
            var consignment = await _consignmentRepository.GetShippedConsignmentByTrackingIdAsync(trackingId);

            return consignment;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim abrufen des Consignment mit der TrackingId '{trackingId}'.");
            return new Consignment();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim abrufen des Consignment mit der TrackingId '{trackingId}'.");
            return new Consignment();
        }
    }
    
    public async Task<bool> UpdateDpdConsignmentStatusAsync(string newStatus, string trackingId)
    {
        try
        {
            string status;

            if (newStatus.Equals(SharedStatus.delivery_customer) || newStatus.Equals(SharedStatus.pickup_by_consignee))
            {
                status = SharedStatus.Delivered;
            }
            else
            {
                return false;
            }

            await _consignmentRepository.UpdateConsignmentStatusByTrackingIdAsync(status, trackingId);

            _logger.LogInformation(
                $"Für das Consignment mit der TrackingId '{trackingId}' wurde der Status in der Datenbank erfolgreich auf '{status}' aktualisiert.");

            return true;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Consignment mit der TrackingId '{trackingId}'.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Consignment mit der TrackingId '{trackingId}'.");
            return false;
        }
    }
    
    public async Task<bool> UpdateConsignmentStatusAsync(string newStatus, Consignment consignment)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht den Status der Consignment mit der Id '{consignment.Id}' in der Datenbank auf '{newStatus}' zu aktualisiert.");

            consignment.Status = newStatus;
            await _consignmentRepository.UpdateConsignmentAsync(consignment);

            _logger.LogInformation(
                $"Der Status der Consignment mit der Id '{consignment.Id}' wurde in der Datenbank erfolgreich auf '{newStatus}' aktualisiert.");

            return true;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{consignment.Status}' für das Consignment mit der Id '{consignment.Id}' in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisierung des Status der Lieferung mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}' auf '{consignment.Status}'.");
            throw new ConsignmentServiceException(
                $"Unerwarteter Fehler beim aktualisierung des Status der Lieferung mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}' auf '{consignment.Status}'.",
                ex);
        }
    }
    
    public async Task<List<Consignment>> SearchConsignmentsAsync(SearchTerm searchTerm, string status)
    {
        if (string.IsNullOrWhiteSpace(searchTerm.value))
        {
            return new List<Consignment>();
        }

        if (string.IsNullOrEmpty(status))
        {
            _logger.LogError($"'{nameof(status)}' darf nicht null sein.");
            throw new ArgumentException(nameof(status));
        }

        try
        {
            await _searchTermValidator.ValidateAndThrowAsync(searchTerm);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex,
                $"Es ist ein Validierungsfehler aufgetreten beim suchen von Consignment mit dem SearchTearm '{searchTerm.value}' aufgetreten.");
            throw;
        }

        try
        {
            return await _consignmentRepository.SearchShippedConsignmentsAsync(searchTerm);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von allen Consignments mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von allen Consignments mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.");
            throw new ConsignmentServiceException(
                $"Unerwarteter Fehler beim Abrufen von allen Consignments mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.",
                ex);
        }
    }
    
    public async Task<List<Consignment>> GetAllConsignmentsByStatusAsync(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            _logger.LogError($"'{nameof(status)}' darf nicht null oder leer sein.");
            throw new ArgumentException($"'{nameof(status)}' darf nicht null oder leer sein.");
        }

        try
        {
            return await _consignmentRepository.GetConsignmentsWithStatusAsync(status);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von allen Consignments mit dem Status '{status}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von allen Consignments mit dem Status '{status}'.");
            throw new ConsignmentServiceException(
                $"Unerwarteter Fehler beim Abrufen von allen Consignments mit dem Status '{status}'.", ex);
        }
    }
    
    public bool AreAllConsignmentsCancelled(Order? order)
    {
        if (order == null)
        {
            _logger.LogError("Order ist null.");
            throw new OrderIsNullException(nameof(order));
        }

        if (order.Consignments.All(consignment => consignment.Status == SharedStatus.Cancelled))
        {
            return true;
        }

        return false;
    }
    
    public async Task<ConsignmentEntry?> GetConsignmentEntryByIdAsync(int consignmentEntryId)
    {
        if (consignmentEntryId <= 0)
        {
            _logger.LogError($"'{nameof(consignmentEntryId)}' muss größer als 0 sein.");
            throw new InvalidIdException($"'{nameof(consignmentEntryId)}' muss größer als 0 sein.");
        }

        try
        {
            return await _consignmentRepository.GetConsignmentEntryByIdAsync(consignmentEntryId);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von ConsignmentEntry mit der Id '{consignmentEntryId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von ConsignmentEntry mit dem Status '{consignmentEntryId}'.");
            throw new ConsignmentServiceException(
                $"Unerwarteter Fehler beim Abrufen von ConsignmentEntry mit dem Status '{consignmentEntryId}'.",
                ex);
        }    
    }
    
    public async Task UpdateConsignmentEntryQuantityAsync(ConsignmentEntry? consignmentEntry, ReturnEntryModel returnEntryModel)
    {
        if (consignmentEntry == null)
        {
            _logger.LogError($"'{nameof(consignmentEntry)}' darf nicht null sein.");
            throw new ConsignmentEntryIsNullException(nameof(consignmentEntry));
        }

        if (returnEntryModel == null)
        {
            _logger.LogError($"'{nameof(returnEntryModel)}' darf nicht null sein.");
            throw new ReturnEntryIsNullException(nameof(returnEntryModel));
        }

        if (returnEntryModel.ReturnQuantity >= consignmentEntry.Quantity)
        {
            consignmentEntry.CancelledOrReturnedQuantity = consignmentEntry.Quantity; // Position komplett Stornieren
        }
        else
        {
            consignmentEntry.CancelledOrReturnedQuantity += returnEntryModel.ReturnQuantity; // Menge der Position erhöhen
        }

        try
        {
            _logger.LogInformation(
                $"Es wird versucht, die Quantity des ConsignmentEntry mit der Id '{consignmentEntry.Id}' in der Datenbank zu aktualisieren.");

            await _consignmentRepository.UpdateConsignmentEntryAsync(consignmentEntry);

            _logger.LogInformation(
                $"Es wurde erfolgreich die Quantity des ConsignmentEntry mit der Id '{consignmentEntry.Id}' in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{consignmentEntry.Id}' in der Datenbank.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{consignmentEntry.Id}' in der Datenbank.");
            throw new ConsignmentServiceException(
                $"Unerwarteter Fehler beim aktualisieren der Quantity des ConsignmentEntry mit der Id '{consignmentEntry.Id}' in der Datenbank.",
                ex);
        }
    }
}