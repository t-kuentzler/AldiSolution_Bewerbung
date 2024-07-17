using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly IValidatorWrapper<ShipmentInfo> _shipmentInfoValidator;
    private readonly IValidatorWrapper<ReceivingReturnRequest> _receivingReturnRequestValidator;
    private readonly TrackingLinkBaseUrls _trackingLinkBaseUrls;
    private readonly IReturnConsignmentAndPackageService _returnConsignmentAndPackageService;


    public ReturnService(ILogger<ReturnService> logger, IReturnRepository returnRepository,
        IRmaNumberGenerator rmaNumberGenerator, IOrderService orderService,
        IQuantityCheckService quantityCheckService, IOAuthClientService oAuthClientService,
        IValidatorWrapper<Return> returnValidator, IConsignmentService consignmentService,
        IValidatorWrapper<SearchTerm> searchTermValidator, IValidatorWrapper<ShipmentInfo> shipmentInfoValidator,
        IValidatorWrapper<ReceivingReturnRequest> receivingReturnRequestValidator, IOptions<TrackingLinkBaseUrls> trackingLinkBaseUrls,
        IReturnConsignmentAndPackageService returnConsignmentAndPackageService)
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
        _shipmentInfoValidator = shipmentInfoValidator;
        _receivingReturnRequestValidator = receivingReturnRequestValidator;
        _trackingLinkBaseUrls = trackingLinkBaseUrls.Value;
        _returnConsignmentAndPackageService = returnConsignmentAndPackageService;
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
    
    public async Task<Return?> GetReturnByIdAsync(int returnId)
    {
        if (returnId <= 0)
        {
            _logger.LogError($"'{nameof(returnId)}' muss größer als 0 sein.");
            throw new ArgumentException($"'{nameof(returnId)}' muss größer als 0 sein.");
        }

        try
        {
            return await _returnRepository.GetReturnByIdAsync(returnId);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, $"Repository-Exception beim Abrufen von Retoure mit der Id '{returnId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unerwarteter Fehler beim Abrufen von Retoure mit dem Status '{returnId}'.");
            throw new ReturnServiceException(
                $"Unerwarteter Fehler beim Abrufen von Retoure mit dem Status '{returnId}'.", ex);
        }
    }
    
    public List<ShipmentInfo?> CreateShipmentInfos(Return returnObj)
    {
        List<ShipmentInfo?> shipmentInfos = new List<ShipmentInfo?>();

        foreach (var returnEntry in returnObj.ReturnEntries)
        {
            foreach (var returnConsignment in returnEntry.ReturnConsignments)
            {
                foreach (var returnPackage in returnConsignment.Packages)
                {
                    var orderEntry = returnObj.Order.Entries.FirstOrDefault(oe =>
                        oe.EntryNumber == returnEntry.OrderEntryNumber &&
                        oe.OrderId == returnObj.Order.Id);

                    if (orderEntry == null)
                    {
                        throw new ArgumentNullException(nameof(orderEntry));
                    }

                    ShipmentInfo shipmentInfo = new ShipmentInfo()
                    {
                        ProductCode = orderEntry.VendorProductCode,
                        Reason = returnEntry.Reason ?? string.Empty,
                        TrackingNumber = returnPackage.TrackingId,
                        Carrier = returnConsignment.Carrier,
                        Quantity = returnConsignment.Quantity,
                        ReturnEntryId = returnEntry.Id
                    };

                    shipmentInfos.Add(shipmentInfo);
                }
            }
        }

        return shipmentInfos;
    }
    
    public async Task ProcessShipmentInfoCreation(ShipmentInfoAndReturnIdRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Die Anfrage darf nicht null sein.");
        }

        var returnObj = await GetReturnByIdAsync(request.ReturnId);
        if (returnObj == null)
        {
            throw new ReturnIsNullException("Das zugehörige Return-Objekt wurde nicht gefunden.");
        }

        // Vorbereitung des Request-Objekts für die API
        var parsedReceivingReturnRequest = await ParseShipmentInfoAndReturnToReceivingReturnRequest(
            request.ShipmentInfo, returnObj
        );

        // Senden der Daten an die API
        var (apiResult, receivingReturnResponse) = await _oAuthClientService.CreateReceivingReturn(parsedReceivingReturnRequest);

        if (!apiResult)
        {
            throw new Exception("Fehler bei der Kommunikation mit der API.");
        }

        // Verarbeiten der API-Antwort und Aktualisieren der Datenbank
        var returnObjWithConsignmentsAndPackages = await _returnConsignmentAndPackageService.CreateReturnConsignmentAndReturnPackage(
            receivingReturnResponse, returnObj
        );
        
        returnObjWithConsignmentsAndPackages =
            await UpdateReturnEntriesQuantity(request.ShipmentInfo, returnObj);

        await UpdateReturnAsync(returnObjWithConsignmentsAndPackages);
        
        var allEntriesAreReceiving = AllReturnEntriesAreReceiving(returnObjWithConsignmentsAndPackages);
        if (allEntriesAreReceiving)
        {
            await UpdateReturnStatusAsync(returnObjWithConsignmentsAndPackages.Id,
                SharedStatus.Receiving);
        }
    }
    
    public async Task<ReceivingReturnRequest> ParseShipmentInfoAndReturnToReceivingReturnRequest(
        List<ShipmentInfo> shipmentInfos, Return returnObj)
    {
        try
        {
            await _returnValidator.ValidateAndThrowAsync(returnObj);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für Return: {ex.Message}");
            throw;
        }

        for (int i = 0; i < shipmentInfos.Count; i++)
        {
            try
            {
                await _shipmentInfoValidator.ValidateAndThrowAsync(shipmentInfos[i]);
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, $"Validierungsfehler für ShipmentInfo an Position {i}: {ex.Message}");
                throw;
            }
        }

        var receivingReturnRequest = CreateReceivingReturnRequest(shipmentInfos, returnObj);

        try
        {
            await _receivingReturnRequestValidator.ValidateAndThrowAsync(receivingReturnRequest);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für ReceivingReturnRequest: {ex.Message}");
            throw;
        }

        return receivingReturnRequest;
    }
    
    private ReceivingReturnRequest CreateReceivingReturnRequest(
        List<ShipmentInfo> shipmentInfos, Return returnObj)
    {
        var receivingReturnRequest = new ReceivingReturnRequest()
        {
            aldiReturnCode = returnObj.AldiReturnCode,
            customerInfo = new ReceivingReturnCustomerInfoRequest()
            {
                address = returnObj.CustomerInfo.Address != null
                    ? new ReceivingReturnAddressRequest()
                    {
                        countryIsoCode = returnObj.CustomerInfo.Address.CountryIsoCode,
                        firstName = returnObj.CustomerInfo.Address.FirstName,
                        lastName = returnObj.CustomerInfo.Address.LastName,
                        streetName = returnObj.CustomerInfo.Address.StreetName,
                        streetNumber = returnObj.CustomerInfo.Address.StreetNumber,
                        postalCode = returnObj.CustomerInfo.Address.PostalCode,
                        town = returnObj.CustomerInfo.Address.Town,
                        type = returnObj.CustomerInfo.Address.Type
                    }
                    : new ReceivingReturnAddressRequest(),
                emailAddress = returnObj.CustomerInfo.EmailAddress,
                phoneNumber = returnObj.CustomerInfo.PhoneNumber ?? string.Empty
            },
            entries = new List<ReceivingReturnEntriesRequest>(),
            initiationDate = returnObj.InitiationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            orderCode = returnObj.OrderCode
        };

        foreach (var shipmentInfo in shipmentInfos)
        {
            var orderEntry =
                returnObj.Order?.Entries?.FirstOrDefault(e => e.VendorProductCode == shipmentInfo.ProductCode);
            var returnEntry = returnObj.ReturnEntries?.FirstOrDefault(e => e.Id == shipmentInfo.ReturnEntryId);

            if (orderEntry == null || returnEntry == null) continue;

            var entryRequest = new ReceivingReturnEntriesRequest()
            {
                consignments = new List<ReceivingReturnConsignmentsRequest>(),
                entryCode = returnEntry.EntryCode ?? string.Empty,
                notes = returnEntry.Notes ?? string.Empty,
                orderEntryNumber = orderEntry.EntryNumber,
                quantity = returnEntry.Quantity,
                reason = returnEntry.Reason ?? string.Empty
            };

            var consignmentRequest = new ReceivingReturnConsignmentsRequest()
            {
                carrier = shipmentInfo.Carrier,
                packages = new List<ReceivingReturnPackagesRequest>(),
                quantity = shipmentInfo.Quantity
            };

            var packageRequest = new ReceivingReturnPackagesRequest()
            {
                status = SharedStatus.Receiving,
                trackingId = shipmentInfo.TrackingNumber,
                trackingLink = CreateTrackingLink(shipmentInfo.TrackingNumber, shipmentInfo.Carrier),
                vendorPackageCode = shipmentInfo.TrackingNumber,
            };

            consignmentRequest.packages.Add(packageRequest);
            entryRequest.consignments.Add(consignmentRequest);
            receivingReturnRequest.entries.Add(entryRequest);
        }

        return receivingReturnRequest;
    }
    
    private string CreateTrackingLink(string trackingId, string carrier)
    {
        var baseUrl = carrier.Equals("DHL") ? _trackingLinkBaseUrls.DHL :
            carrier.Equals("DPD") ? _trackingLinkBaseUrls.DPD : string.Empty;
        return $"{baseUrl}{trackingId}";
    }
    
    private async Task<Return> UpdateReturnEntriesQuantity(List<ShipmentInfo> requestShipmentInfo, Return returnObj)
    {
        for (int i = 0; i < requestShipmentInfo.Count; i++)
        {
            try
            {
                await _shipmentInfoValidator.ValidateAndThrowAsync(requestShipmentInfo[i]);
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex, $"Validierungsfehler für ShipmentInfo an Position {i}: {ex.Message}");
                throw;
            }
        }

        foreach (var shipmentInfo in requestShipmentInfo)
        {
            var returnEntry = returnObj.ReturnEntries.FirstOrDefault(e => e.Id == shipmentInfo.ReturnEntryId);
            if (returnEntry != null)
            {
                returnEntry.CanceledOrReturnedQuantity += shipmentInfo.Quantity;
            }
        }

        try
        {
            await _returnValidator.ValidateAndThrowAsync(returnObj);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für Return: {ex.Message}");
            throw;
        }

        return returnObj;
    }
    
    private async Task UpdateReturnAsync(Return returnObj)
    {
        try
        {
            await _returnRepository.UpdateReturnAsync(returnObj);
            _logger.LogInformation(
                $"Die Retoure mit dem rma code '{returnObj.Rma}' wurde erfolgreich in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren der Retoure mit dem rma code '{returnObj.Rma}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim beim aktualisieren der Retoure mit dem rma code '{returnObj.Rma}'.");
            throw new ReturnServiceException(
                $"Unerwarteter Fehler beim beim aktualisieren der Retoure mit dem rma code '{returnObj.Rma}'.", ex);
        }
    }
    
    private bool AllReturnEntriesAreReceiving(Return returnObjWithConsignmentsAndPackages)
    {
        return returnObjWithConsignmentsAndPackages.ReturnEntries.All(entry =>
            entry.Quantity == entry.CanceledOrReturnedQuantity);
    }
    
    public async Task UpdateReturnStatusAsync(int returnId, string status)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht den Status der Return mit der Id '{returnId}' in der Datenbank auf '{status}' zu aktualisiert.");

            await _returnRepository.UpdateReturnStatusByIdAsync(returnId, status);

            _logger.LogInformation(
                $"Der Status der Return mit der Id '{returnId}' wurde in der Datenbank erfolgreich auf '{status}' aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{status}' für die Return mit der Id '{returnId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die Return mit der Id '{returnId}'.");
            throw new ReturnServiceException(
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die Return mit der Id '{returnId}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnConsignmentAndPackagesStatusAsync(string consignmentCode, string status)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht den Status der ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}' in der Datenbank auf '{status}' zu aktualisiert.");

            await _returnRepository.UpdateReturnConsignmentAndPackagesStatusAsync(consignmentCode, status);

            _logger.LogInformation(
                $"Der Status der ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}' wurde in der Datenbank erfolgreich auf '{status}' aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{status}' für die ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}'.");
            throw new ReturnServiceException(
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die ReturnConsignment und ReturnPackages mit dem ConsignmentCode '{consignmentCode}'.",
                ex);
        }
    }
    
    public async Task UpdateReturnPackagesReceiptDeliveryAsync(string consignmentCode)
    {
        try
        {
            var returnConsignment = await _returnConsignmentAndPackageService.GetReturnConsignmentByConsignmentCodeAsync(consignmentCode);
            DateTime receiptDelivery = DateTime.UtcNow;

            foreach (var package in returnConsignment.Packages)
            {
                package.ReceiptDelivery = receiptDelivery;
            }

            await _returnConsignmentAndPackageService.UpdateReturnConsignmentAsync(returnConsignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler beim aktualisieren des ReceiptDelivery für ein die ReturnPackages der ReturnConsignment mit dem ConsignmentCode '{consignmentCode}'.");
            throw new ReturnServiceException(
                $"Fehler beim aktualisieren des ReceiptDelivery für ein die ReturnPackages der ReturnConsignment mit dem ConsignmentCode '{consignmentCode}'.",
                ex);
        }
    }
    
    public async Task<bool> CheckIfAllConsignmentsAreReceived(int returnId)
    {
        var returnObj = await GetReturnByIdAsync(returnId);
        if (returnObj == null)
        {
            _logger.LogError($"Return mit der Id '{returnId}' wurde in der Datenbank nicht gefunden.");
            throw new ArgumentNullException($"Return mit der Id '{returnId}' wurde in der Datenbank nicht gefunden.");
        }

        // Zugriff auf ReturnEntries und dann auf ReturnConsignments und Packages
        if (returnObj.ReturnEntries == null || !returnObj.ReturnEntries.Any())
        {
            _logger.LogError($"Keine ReturnEntries für die Return mit der Id '{returnId}' in der Datenbank gefunden.");
            throw new ArgumentNullException(
                $"Keine ReturnEntries für die Return mit der Id '{returnId}' in der Datenbank gefunden.");
        }

        foreach (var entry in returnObj.ReturnEntries)
        {
            if (entry.ReturnConsignments == null || !entry.ReturnConsignments.Any())
            {
                _logger.LogError(
                    $"Keine ReturnConsignments für die Return mit der Id '{returnId}' in der Datenbank gefunden.");
                throw new ArgumentNullException(
                    $"Keine ReturnConsignments für die Return mit der Id '{returnId}' in der Datenbank gefunden.");
            }

            foreach (var consignment in entry.ReturnConsignments)
            {
                if (consignment.Status != SharedStatus.Received)
                {
                    return false;
                }
            }
        }

        // Alle Consignments haben den Status "RECEIVED"
        return true;
    }
    
    public async Task<bool> ProcessPackageStatusUpdateAsync(PackageStatusUpdateRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Die Anfrage enthält keine gültigen Daten.");
        }

        var returnObj = await GetReturnByIdAsync(request.ReturnId);
        if (returnObj == null)
        {
            throw new ReturnIsNullException("Return ist null.");
        }

        bool apiResult = false;
        if (request.Status.Equals(SharedStatus.Completed))
        {
            var completedReturnPackageRequest = CreateCompletedReturnPackageRequestAsync(returnObj);
            apiResult = await _oAuthClientService.ReportReturnPackage(completedReturnPackageRequest);
        }
        else if (request.Status.Equals(SharedStatus.Canceled))
        {
            var canceledReturnPackageRequest = CreateCanceledReturnPackageRequestAsync(returnObj);
            apiResult = await _oAuthClientService.ReportReturnPackage(canceledReturnPackageRequest);
        }

        if (apiResult)
        {
            await _returnConsignmentAndPackageService.UpdateAllReturnPackageStatusFromReturnAsync(SharedStatus.Completed, returnObj);
            await _returnConsignmentAndPackageService.UpdateReturnConsignmentStatusQuantityAsync(request.Status, returnObj);
            await _returnConsignmentAndPackageService.UpdateCompletedDateForAllReturnConsignments(returnObj);
            await UpdateReturnStatusAsync(request.ReturnId, SharedStatus.Completed);
        }
        else
        {
            throw new Exception("Es ist ein unerwarteter API Fehler aufgetreten.");
        }

        return true;
    }
    
    private ReportReturnPackageRequest CreateCompletedReturnPackageRequestAsync(Return returnObj)
    {
        if (returnObj == null)
        {
            _logger.LogError("Return darf nicht null sein.");
            throw new ArgumentNullException(nameof(returnObj));
        }

        if (returnObj.CustomerInfo.Address == null)
        {
            _logger.LogError("Address darf nicht null sein.");
            throw new ArgumentNullException(nameof(returnObj.CustomerInfo.Address));
        }

        var completedReturnPackageRequest = new ReportReturnPackageRequest
        {
            aldiReturnCode = returnObj.AldiReturnCode,
            customerInfo = new ReportReturnPackageCustomerInfoRequest
            {
                emailAddress = returnObj.CustomerInfo.EmailAddress,
                phoneNumber = returnObj.CustomerInfo.PhoneNumber ?? string.Empty,
                address = new ReportReturnPackageAddressRequest
                {
                    countryIsoCode = returnObj.CustomerInfo.Address.CountryIsoCode,
                    type = returnObj.CustomerInfo.Address.Type
                }
            },
            entries = returnObj.ReturnEntries.Select(entry => new ReportReturnPackageEntryRequest
            {
                consignments = entry.ReturnConsignments.Select(consignment => new ReportReturnPackageConsignmentRequest
                {
                    carrier = consignment.Carrier,
                    consignmentCode = consignment.ConsignmentCode,
                    packages = consignment.Packages.Select(package => new ReportReturnPackageDetailsRequest
                    {
                        completedDate =
                            package.ReturnConsignment.CompletedDate?.ToString("yyyy-MM-dd",
                                CultureInfo.InvariantCulture) ?? string.Empty,
                        receiptDelivery =
                            package.ReceiptDelivery?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ??
                            string.Empty,
                        status = SharedStatus.Completed,
                        trackingId = package.TrackingId,
                        trackingLink = package.TrackingLink,
                        vendorPackageCode = package.VendorPackageCode
                    }).ToList(),
                    quantity = consignment.Quantity
                }).ToList(),
                entryCode = entry.EntryCode ?? string.Empty,
                notes = entry.Notes ?? string.Empty,
                orderEntryNumber = entry.OrderEntryNumber,
                quantity = entry.Quantity,
                reason = entry.Reason ?? string.Empty
            }).ToList(),
            initiationDate = returnObj.InitiationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            orderCode = returnObj.OrderCode
        };

        return completedReturnPackageRequest;
    }
    
    private ReportReturnPackageRequest CreateCanceledReturnPackageRequestAsync(Return returnObj)
    {
        if (returnObj.CustomerInfo.Address == null)
        {
            _logger.LogError("Address darf nicht null sein.");
            throw new ArgumentNullException(nameof(returnObj.CustomerInfo.Address));
        }

        var canceledReturnPackageRequest = new ReportReturnPackageRequest
        {
            aldiReturnCode = returnObj.AldiReturnCode,
            customerInfo = new ReportReturnPackageCustomerInfoRequest
            {
                emailAddress = returnObj.CustomerInfo.EmailAddress,
                address = new ReportReturnPackageAddressRequest
                {
                    countryIsoCode = returnObj.CustomerInfo.Address.CountryIsoCode,
                    type = returnObj.CustomerInfo.Address.Type
                }
            },
            entries = returnObj.ReturnEntries.Select(entry => new ReportReturnPackageEntryRequest
            {
                consignments = entry.ReturnConsignments.Select(consignment => new ReportReturnPackageConsignmentRequest
                {
                    carrier = consignment.Carrier,
                    consignmentCode = consignment.ConsignmentCode,
                    packages = consignment.Packages.Select(package => new ReportReturnPackageDetailsRequest
                    {
                        status = SharedStatus.Canceled,
                        trackingId = package.TrackingId,
                        trackingLink = package.TrackingLink,
                        vendorPackageCode = package.VendorPackageCode
                    }).ToList(),
                    quantity = entry.Quantity
                }).ToList(),
                entryCode = entry.EntryCode ?? string.Empty,
                notes = entry.Notes ?? string.Empty,
                orderEntryNumber = entry.OrderEntryNumber,
                quantity = entry.Quantity,
                reason = entry.Reason ?? string.Empty
            }).ToList(),
            initiationDate = returnObj.InitiationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
            orderCode = returnObj.OrderCode
        };

        return canceledReturnPackageRequest;
    }
}