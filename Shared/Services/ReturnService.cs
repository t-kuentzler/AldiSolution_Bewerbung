using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

public class ReturnService : IReturnService
{
    private readonly ILogger<ReturnService> _logger;
    private readonly IReturnRepository _returnRepository;
    private readonly IRmaNumberGenerator _rmaNumberGenerator;
    private readonly IOrderService _orderService;
    private readonly IQuantityCheckService _quantityCheckService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly IValidatorWrapper<Return> _returnValidator;
    private readonly IConsignmentService _consignmentService;
    private readonly IValidatorWrapper<SearchTerm> _searchTermValidator;

    public ReturnService(ILogger<ReturnService> logger, IReturnRepository returnRepository,
        IRmaNumberGenerator rmaNumberGenerator, IOrderService orderService,
        IQuantityCheckService quantityCheckService, IOAuthClientService oAuthClientService,
        IValidatorWrapper<Return> returnValidator, IConsignmentService consignmentService,
        IValidatorWrapper<SearchTerm> searchTermValidator)
    {
        _logger = logger;
        _returnRepository = returnRepository;
        _rmaNumberGenerator = rmaNumberGenerator;
        _orderService = orderService;
        _quantityCheckService = quantityCheckService;
        _oAuthClientService = oAuthClientService;
        _returnValidator = returnValidator;
        _consignmentService = consignmentService;
        _searchTermValidator = searchTermValidator;
    }

    public List<Return> ParseReturnResponseToReturnObject(ReturnResponse returnResponse)
    {
        List<Return> returnList = new List<Return>();

        foreach (var returnRequest in returnResponse.ReturnRequests)
        {
            var returnObj = new Return
            {
                OrderCode = returnRequest.OrderCode,
                InitiationDate = returnRequest.InitiationDate,
                AldiReturnCode = returnRequest.Code,
                Status = SharedStatus.InProgress,
                Rma = _rmaNumberGenerator.GenerateRma(returnRequest.OrderCode),

                CustomerInfo = new CustomerInfo
                {
                    EmailAddress = returnRequest.CustomerEmailAddress,
                    PhoneNumber = returnRequest.CustomerPhoneNumber,
                    Address = new Address
                    {
                        Type = returnRequest.Address.Type,
                        SalutationCode = returnRequest.Address.SalutationCode ?? string.Empty,
                        FirstName = returnRequest.Address.FirstName,
                        LastName = returnRequest.Address.LastName,
                        StreetName = returnRequest.Address.StreetName,
                        StreetNumber = returnRequest.Address.StreetNumber,
                        Remarks = returnRequest.Address.Remarks ?? string.Empty,
                        PostalCode = returnRequest.Address.PostalCode,
                        Town = returnRequest.Address.Town,
                        PackstationNumber = returnRequest.Address.PackstationNumber ?? string.Empty,
                        PostNumber = returnRequest.Address.PostNumber ?? string.Empty,
                        PostOfficeNumber = returnRequest.Address.PostOfficeNumber ?? string.Empty,
                        CountryIsoCode = returnRequest.Address.CountryIsoCode
                    }
                },

                ReturnEntries = new List<ReturnEntry>()
            };

            foreach (var requestEntry in returnRequest.Entries)
            {
                var entry = new ReturnEntry
                {
                    Reason = requestEntry.Reason,
                    Notes = CleanNotes(requestEntry.Notes),
                    Status = SharedStatus.InProgress,
                    OrderEntryNumber = requestEntry.OrderEntryNumber,
                    Quantity = requestEntry.Quantity,
                    EntryCode = requestEntry.Code,
                    CarrierCode = requestEntry.CarrierCode
                };

                returnObj.ReturnEntries.Add(entry);
            }

            returnList.Add(returnObj);
        }

        return returnList;
    }

    // Sonderzeichen entfernen, da API diese nicht akzeptiert
    private string CleanNotes(string? notes)
    {
        if (notes == null)
            return string.Empty;

        return Regex.Replace(notes, @"[^a-zA-Z0-9\s]", "");
    }

    public async Task<bool> CreateReturnAsync(Return returnObj)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht, die Retoure mit dem AldiReturnCode '{returnObj.AldiReturnCode}' in der Datenbank zu erstellen.");
            await _returnRepository.CreateReturnAsync(returnObj);
            _logger.LogInformation(
                $"Die Retoure mit dem AldiReturnCode '{returnObj.AldiReturnCode}' wurde erfolgreich in der Datenbank erstellt.");

            return true; // Erfolg signalisieren
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Erstellen der Retoure. Rma code: '{returnObj.Rma}', OrderCode: '{returnObj.OrderCode}', AldiReturnCode: '{returnObj.AldiReturnCode}'.");
            throw new ReturnServiceException(
                $"Fehler beim Erstellen der Retoure. Rma code: '{returnObj.Rma}', OrderCode: '{returnObj.OrderCode}', AldiReturnCode: '{returnObj.AldiReturnCode}'.",
                ex);
        }
    }

    public ReturnInProgressRequest? ParseReturnToReturnInProgressRequest(Return returnObj)
    {
        if (returnObj.CustomerInfo.Address == null)
        {
            _logger.LogError("Address ist null.");
            return new ReturnInProgressRequest();
        }

        var request = new ReturnInProgressRequest()
        {
            aldiReturnCode = returnObj.AldiReturnCode,
            customerInfo = new ReturnInProgressCustomerInfoRequest()
            {
                emailAddress = returnObj.CustomerInfo.EmailAddress,
                address = new ReturnInProgressAddressRequest()
                {
                    type = returnObj.CustomerInfo.Address.Type,
                    salutationCode = returnObj.CustomerInfo.Address.SalutationCode,
                    firstName = returnObj.CustomerInfo.Address.FirstName,
                    lastName = returnObj.CustomerInfo.Address.LastName,
                    streetName = returnObj.CustomerInfo.Address.StreetName ?? string.Empty,
                    streetNumber = returnObj.CustomerInfo.Address.StreetNumber ?? string.Empty,
                    town = returnObj.CustomerInfo.Address.Town,
                    postalCode = returnObj.CustomerInfo.Address.PostalCode,
                    packstationNumber = returnObj.CustomerInfo.Address.PackstationNumber,
                    remarks = returnObj.CustomerInfo.Address.Remarks,
                    postNumber = returnObj.CustomerInfo.Address.PostNumber,
                    postOfficeNumber = returnObj.CustomerInfo.Address.PostOfficeNumber,
                    countryIsoCode = returnObj.CustomerInfo.Address.CountryIsoCode,
                }
            },
            entries = new List<ReturnInProgressEntryRequest>(),
            initiationDate = returnObj.InitiationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            orderCode = returnObj.OrderCode
        };

        foreach (var entry in returnObj.ReturnEntries)
        {
            var entryRequest = new ReturnInProgressEntryRequest()
            {
                entryCode = entry.EntryCode,
                notes = entry.Notes ?? string.Empty,
                orderEntryNumber = entry.OrderEntryNumber,
                quantity = entry.Quantity,
                reason = entry.Reason ?? string.Empty,
                status = SharedStatus.InProgress
            };

            request.entries.Add(entryRequest);
        }

        return request;
    }

    public async Task<ReturnProcessingResult> ProcessReturn(int orderId,
        Dictionary<int, ReturnEntryModel> returnEntries)
    {
        if (!returnEntries.Any(entry => entry.Value.IsReturned))
        {
            return new ReturnProcessingResult
                { Success = false, ErrorMessage = "Bitte wählen Sie mindestens einen Artikel für die Retoure aus." };
        }

        foreach (var entry in returnEntries)
        {
            if (entry.Value.IsReturned && entry.Value.ReturnQuantity <= 0)
            {
                return new ReturnProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Bitte geben Sie eine gültige Menge für die ausgewählten Artikel ein."
                };
            }

            if (entry.Value.IsReturned && string.IsNullOrEmpty(entry.Value.Reason))
            {
                return new ReturnProcessingResult
                {
                    Success = false, ErrorMessage = "Bitte wählen Sie einen Grund für die ausgewählten Artikel aus."
                };
            }
        }

        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return new ReturnProcessingResult { Success = false, ErrorMessage = "Bestellung nicht gefunden." };
        }

        foreach (var returnEntry in returnEntries)
        {
            if (!returnEntry.Value.IsReturned) continue;

            var consignmentEntry = order.Consignments
                .SelectMany(c => c.ConsignmentEntries)
                .FirstOrDefault(ce => ce.Id == returnEntry.Value.ConsignmentEntryId);

            if (consignmentEntry != null && _quantityCheckService.IsQuantityExceedingAvailable(
                    consignmentEntry.CancelledOrReturnedQuantity,
                    returnEntry.Value.ReturnQuantity, consignmentEntry.Quantity))
            {
                return new ReturnProcessingResult
                    { Success = false, ErrorMessage = "Die Retoure Menge überschreitet die verfügbare Menge." };
            }
        }

        var customerInfo = CreateCustomerInfoFromOrder(order);

        var groupedEntries = GroupReturnEntries(returnEntries);

        var returnEntriesRequests = CreateReturnEntryRequests(groupedEntries);

        var manualReturnRequest = CreateManualReturnRequest(order, customerInfo, returnEntriesRequests);

        var (apiResult, manualReturnResponse) =
            await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        if (!apiResult)
        {
            return new ReturnProcessingResult
                { Success = false, ErrorMessage = "Es ist ein Fehler bei dem Aufruf der API aufgetreten." };
        }

        return new ReturnProcessingResult { Success = true, ManualReturnResponse = manualReturnResponse };
    }
    
    private CustomerInfoRequest CreateCustomerInfoFromOrder(Order order)
    {
        var firstEntry = order.Entries?.FirstOrDefault();
        var deliveryAddress = firstEntry?.DeliveryAddress;

        var customerInfo = new CustomerInfoRequest()
        {
            address = new AddressRequest()
            {
                countryIsoCode = deliveryAddress?.CountryIsoCode ?? string.Empty,
                firstName = deliveryAddress?.FirstName ?? string.Empty,
                lastName = deliveryAddress?.LastName ?? string.Empty,
                packstationNumber = deliveryAddress?.PackstationNumber ?? string.Empty,
                postNumber = deliveryAddress?.PostNumber ?? string.Empty,
                postOfficeNumber = deliveryAddress?.PostOfficeNumber ?? string.Empty,
                postalCode = deliveryAddress?.PostalCode ?? string.Empty,
                remarks = deliveryAddress?.Remarks ?? string.Empty,
                salutationCode = deliveryAddress?.SalutationCode ?? string.Empty,
                streetName = deliveryAddress?.StreetName ?? string.Empty,
                streetNumber = deliveryAddress?.StreetNumber ?? string.Empty,
                town = deliveryAddress?.Town ?? string.Empty,
                type = deliveryAddress?.Type ?? "DEFAULT"
            },
            emailAddress = order.EmailAddress ?? string.Empty,
            phoneNumber = order.Phone ?? string.Empty
        };

        return customerInfo;
    }
    
    private List<ReturnEntryModel> GroupReturnEntries(Dictionary<int, ReturnEntryModel> returnEntries)
    {
        return returnEntries
            .Where(e => e.Value.IsReturned)
            .GroupBy(e => new { e.Value.OrderEntryNumber, e.Value.Reason })
            .Select(g => new ReturnEntryModel
            {
                OrderEntryNumber = g.Key.OrderEntryNumber,
                ReturnQuantity = g.Sum(e => e.Value.ReturnQuantity),
                Reason = g.First().Value.Reason,
                IsReturned = true
            })
            .ToList();
    }
    
    private List<ReturnEntryRequest> CreateReturnEntryRequests(List<ReturnEntryModel> groupedEntries)
    {
        var returnEntriesRequests = new List<ReturnEntryRequest>();
        foreach (var entry in groupedEntries)
        {
            if (entry.IsReturned)
            {
                returnEntriesRequests.Add(new ReturnEntryRequest()
                {
                    notes = "Retoure durch Kunden",
                    orderEntryNumber = entry.OrderEntryNumber,
                    quantity = entry.ReturnQuantity,
                    reason = entry.Reason,
                    status = SharedStatus.InProgress
                });
            }
        }
        return returnEntriesRequests;
    }
    
    private ManualReturnRequest CreateManualReturnRequest(Order order, CustomerInfoRequest customerInfo, List<ReturnEntryRequest> returnEntriesRequests)
    {
        return new ManualReturnRequest()
        {
            customerInfo = customerInfo,
            entries = returnEntriesRequests,
            initiationDate = DateTime.UtcNow,
            orderCode = order.Code,
            rma = _rmaNumberGenerator.GenerateRma(order.Code),
        };
    }
    
    public async Task ProcessManualReturnAsync(int orderId, ManualReturnResponse manualReturnResponse, Dictionary<int, ReturnEntryModel> returnEntries)
    {
        try
        {
            var parsedReturn = await ParseManualReturnToReturnObject(manualReturnResponse);
            
            await ProcessReturnEntriesAsync(orderId, returnEntries);
            await CreateReturnAsync(parsedReturn);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReturnServiceException(
                $"Fehler beim Verarbeiten der manuellen Retoure für {nameof(orderId)} '{orderId}'.", ex);
        }
    }
    
    private async Task<Return> ParseManualReturnToReturnObject(ManualReturnResponse manualReturnResponse)
    {
        try
        {
            var returnObj = new Return
            {
                OrderCode = manualReturnResponse.orderCode,
                InitiationDate = manualReturnResponse.initiationDate,
                AldiReturnCode = manualReturnResponse.aldiReturnCode,
                Rma = manualReturnResponse.rma,
                Status = SharedStatus.InProgress,
                CustomerInfo = manualReturnResponse.customerInfo != null
                    ? new CustomerInfo
                    {
                        EmailAddress = manualReturnResponse.customerInfo.emailAddress,
                        PhoneNumber = manualReturnResponse.customerInfo.phoneNumber ?? String.Empty,
                        Address = manualReturnResponse.customerInfo.address != null
                            ? new Address
                            {
                                Type = manualReturnResponse.customerInfo.address.type ?? String.Empty,
                                SalutationCode = manualReturnResponse.customerInfo.address.salutationCode ??
                                                 String.Empty,
                                FirstName = manualReturnResponse.customerInfo.address.firstName ?? String.Empty,
                                LastName = manualReturnResponse.customerInfo.address.lastName ?? String.Empty,
                                StreetName = manualReturnResponse.customerInfo.address.streetName ?? String.Empty,
                                StreetNumber = manualReturnResponse.customerInfo.address.streetNumber ?? String.Empty,
                                Remarks = manualReturnResponse.customerInfo.address.remarks ?? String.Empty,
                                PostalCode = manualReturnResponse.customerInfo.address.postalCode ?? String.Empty,
                                Town = manualReturnResponse.customerInfo.address.town ?? String.Empty,
                                PackstationNumber = manualReturnResponse.customerInfo.address.packstationNumber ??
                                                    String.Empty,
                                PostNumber = manualReturnResponse.customerInfo.address.postNumber ?? String.Empty,
                                PostOfficeNumber = manualReturnResponse.customerInfo.address.postOfficeNumber ??
                                                   String.Empty,
                                CountryIsoCode = manualReturnResponse.customerInfo.address.countryIsoCode ??
                                                 String.Empty
                            }
                            : new Address()
                    }
                    : new CustomerInfo(),
                ReturnEntries = new List<ReturnEntry>()
            };

            if (manualReturnResponse.entries != null)
            {
                foreach (var manualEntry in manualReturnResponse.entries)
                {
                    var entry = new ReturnEntry
                    {
                        Reason = manualEntry.reason ?? string.Empty,
                        Notes = manualEntry.notes ?? string.Empty,
                        OrderEntryNumber = manualEntry.orderEntryNumber,
                        Quantity = manualEntry.quantity,
                        EntryCode = manualEntry.entryCode ?? string.Empty,
                        Status = manualEntry.status,
                        CarrierCode = manualEntry.carrierCode ?? string.Empty,
                        ReturnConsignments = manualEntry.consignments != null
                            ? manualEntry.consignments.Select(manualConsignment => new ReturnConsignment
                            {
                                ConsignmentCode = manualConsignment.consignmentCode,
                                Quantity = manualConsignment.quantity,
                                Carrier = manualConsignment.carrier ?? string.Empty,
                                CompletedDate = manualConsignment.completedDate
                            }).ToList()
                            : new List<ReturnConsignment>()
                    };

                    returnObj.ReturnEntries.Add(entry);
                }
            }

            await _returnValidator.ValidateAndThrowAsync(returnObj);

            return returnObj;
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für Return: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unerwarteter Fehler beim Parsen der manuellen Rückgabe.");
            throw new ReturnServiceException("Unerwarteter Fehler beim Parsen der manuellen Rückgabe.", ex);
        }
    }
    
    //Entry Quantity anpassen damit nicht zu viel returned werden kann
    private async Task ProcessReturnEntriesAsync(int orderId, Dictionary<int, ReturnEntryModel> returnEntries)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        foreach (var entry in returnEntries)
        {
            var consignmentEntry = await _consignmentService.GetConsignmentEntryByIdAsync(entry.Value.ConsignmentEntryId);
            await _consignmentService.UpdateConsignmentEntryQuantityAsync(consignmentEntry, entry.Value);
        }
        
        try
        {
            await _consignmentService.ProcessConsignmentStatusesAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler bei der Verarbeitung der Consignment-Status für die Bestellung mit der Id '{order.Id}'.");
            throw new ReturnServiceException(
                $"Fehler bei der Verarbeitung der Consignment-Status für die Bestellung mit der Id '{order.Id}'.", ex);
        }
    }
    
    public async Task<List<Return>> SearchReturnsAsync(SearchTerm searchTerm, List<string> statuses)
    {
        if (string.IsNullOrWhiteSpace(searchTerm.value))
        {
            return new List<Return>();
        }

        if (statuses == null || !statuses.Any())
        {
            _logger.LogError($"'{nameof(statuses)}' darf nicht null oder leer sein.");
            throw new ArgumentNullException(nameof(statuses), $"'{nameof(statuses)}' darf nicht null oder leer sein.");
        }

        try
        {
            await _searchTermValidator.ValidateAndThrowAsync(searchTerm);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex,
                $"Es ist ein Validierungsfehler aufgetreten beim Suchen von Returns mit dem searchTerm '{searchTerm.value}'.");
            throw;
        }

        var allReturns = new List<Return>();

        try
        {
            foreach (var status in statuses)
            {
                var returnsForStatus = await _returnRepository.SearchReturnsAsync(searchTerm, status);
                // Hinzufügen der Ergebnisse zur Gesamtliste, Duplikate vermeiden
                allReturns.AddRange(returnsForStatus.Where(r => !allReturns.Any(ar => ar.Id == r.Id)));
            }

            return allReturns;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Suchen von Returns mit dem Suchbegriff '{searchTerm.value}' und Status '{string.Join(", ", statuses)}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Suchen von Returns mit dem Suchbegriff '{searchTerm.value}' und Status '{string.Join(", ", statuses)}'.");
            throw new ReturnServiceException(
                $"Unerwarteter Fehler beim Suchen von Returns mit dem Suchbegriff '{searchTerm.value}' und Status '{string.Join(", ", statuses)}'.",
                ex);
        }
    }
    
    public async Task<List<Return>> GetAllReturnsByStatusesAsync(List<string> statuses)
    {
        if (statuses == null || !statuses.Any())
        {
            _logger.LogError($"'{nameof(statuses)}' darf nicht null oder leer sein.");
            throw new ArgumentNullException($"'{nameof(statuses)}' darf nicht null oder leer sein.");
        }

        try
        {
            var returns = new List<Return>();
            foreach (var status in statuses)
            {
                var returnsWithStatus = await _returnRepository.GetReturnsWithStatusAsync(status);
                returns.AddRange(returnsWithStatus);
            }

            return returns.Distinct().ToList(); // Vermeiden von Duplikaten, falls vorhanden
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, $"Repository-Exception beim Abrufen von Retouren.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unerwarteter Fehler beim Abrufen von Retouren.");
            throw new ReturnServiceException($"Unerwarteter Fehler beim Abrufen von Retouren.", ex);
        }
    }
}