using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class OrderService : IOrderService
{
    private readonly ICancellationService _cancellationService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly ILogger<OrderService> _logger;
    private readonly IValidatorWrapper<Order> _orderValidator;
    private readonly IValidatorWrapper<UpdateStatus> _updateStatusValidator;
    private readonly IOrderRepository _orderRepository;
    private readonly IValidatorWrapper<SearchTerm> _searchTermValidator;
    private readonly IValidatorWrapper<CancelOrderEntryModel> _cancelOrderEntryValidator;
    private readonly IQuantityCheckService _quantityCheckService;




    public OrderService(ICancellationService cancellationService, IOAuthClientService oAuthClientService,
        ILogger<OrderService> logger, IValidatorWrapper<Order> orderValidator,
        IValidatorWrapper<UpdateStatus> updateStatusValidator, IOrderRepository orderRepository,
        IValidatorWrapper<SearchTerm> searchTermValidator, IValidatorWrapper<CancelOrderEntryModel> cancelOrderEntryValidator,
        IQuantityCheckService quantityCheckService
        )
    {
        _cancellationService = cancellationService;
        _oAuthClientService = oAuthClientService;
        _logger = logger;
        _orderValidator = orderValidator;
        _updateStatusValidator = updateStatusValidator;
        _orderRepository = orderRepository;
        _searchTermValidator = searchTermValidator;
        _cancelOrderEntryValidator = cancelOrderEntryValidator;
        _quantityCheckService = quantityCheckService;
    }

    public async Task ProcessSingleOrderAsync(Order order)
    {
        try
        {
            await AddOrderAsync(order);
            await UpdateOrderStatusInDatabaseAsync(order, SharedStatus.InProgress);
            await _oAuthClientService.UpdateApiOrderStatusInProgressAsync(order);
        }
        catch (ValidationException ex)
        {
            _logger.LogError($"Fehler bei der Verarbeitung der Bestellung '{order.Code}': {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"Ein unerwarteter Fehler ist bei der Verarbeitung der Bestellung '{order.Code}' aufgetreten: {ex.Message}");
        }
    }

    public async Task<Order> GetOrderByOrderCodeAsync(string orderCode)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            _logger.LogError($"'{nameof(orderCode)}' darf nicht null oder leer sein.");
            throw new ArgumentException($"'{nameof(orderCode)}' darf nicht null oder leer sein.", nameof(orderCode));
        }

        try
        {
            var order = await _orderRepository.GetOrderByOrderCodeAsync(orderCode);
            if (order == null)
            {
                throw new OrderIsNullException("Order darf nicht null sein.");
            }

            return order;
        }
        catch (OrderIsNullException)
        {
            throw;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, $"Repository-Exception beim Abrufen von Order mit dem OrderCode '{orderCode}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unerwarteter Fehler beim Abrufen von Order mit dem OrderCode '{orderCode}'.");
            throw new OrderServiceException(
                $"Unerwarteter Fehler beim Abrufen von Order mit dem OrderCode '{orderCode}'.", ex);
        }
    }

    private async Task AddOrderAsync(Order order)
    {
        _logger.LogInformation("Es wird versucht, eine Bestellung in der Datenbank zu erstellen.");

        if (order == null)
        {
            _logger.LogError("Die zu erstellende Bestellung ist null.");
            throw new ArgumentNullException(nameof(order), "Die zu erstellende Bestellung ist null.");
        }

        try
        {
            await _orderValidator.ValidateAndThrowAsync(order);
            await _orderRepository.CreateOrderAsync(order);
            _logger.LogInformation(
                $"Die Bestellung mit dem OrderCode '{order.Code}' wurde erfolgreich in der Datenbank erstellt.");
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex,
                $"Bei der Bestellung mit dem Code '{order.Code}' ist ein Validierungsfehler aufgetreten: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            var createOrderException = new CreateOrderException(order.Code, ex);
            _logger.LogError(createOrderException.Message);
            throw;
        }
    }

    private async Task UpdateOrderStatusInDatabaseAsync(Order? order, string status)
    {
        if (order == null)
        {
            _logger.LogError("Die übergebene Bestellung ist null.");
            throw new OrderIsNullException();
        }

        var updateStatus = new UpdateStatus
        {
            Code = order.Code,
            Status = status
        };
        await _updateStatusValidator.ValidateAndThrowAsync(updateStatus);


        bool updateSuccess = await _orderRepository.UpdateOrderStatusAsync(order.Code, status);

        if (!updateSuccess)
        {
            throw new UpdateDatabaseException(
                $"Fehler beim Aktualisieren des Status in der Datenbank für Bestellung mit Code '{order.Code}'.");
        }
        else
        {
            _logger.LogInformation(
                $"Bestellung mit Code '{order.Code}' wurde erfolgreich in der Datenbank auf '{status}' aktualisiert.");
        }
    }
    
    public async Task<string> GetOrderStatusByOrderCodeAsync(string orderCode)
    {
        try
        {
            var orderStatus = await _orderRepository.GetOrderStatusByOrderCodeAsync(orderCode);

            if (string.IsNullOrEmpty(orderStatus))
            {
                _logger.LogError($"Der Status für die Order mit dem OrderCode '{orderCode}' ist null oder empty.");

                throw new OrderStatusIsNullException();
            }

            return orderStatus;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, $"Repository-Exception beim Abrufen von Order mit dem OrderCode '{orderCode}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unerwarteter Fehler beim Abrufen von Order mit dem OrderCode '{orderCode}'.");
            throw new OrderServiceException($"Fehler beim Abrufen der Order mit dem OrderCode '{orderCode}'.", ex);
        }
    }
    
    public async Task<bool> UpdateSingleOrderStatusInDatabaseAsync(string orderCode, string status)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            _logger.LogError("OrderCode darf beim aktualisieren des Order Status nicht leer sein.");
            return false;
        }

        try
        {
            var updateStatus = new UpdateStatus
            {
                Code = orderCode,
                Status = status
            };
            await _updateStatusValidator.ValidateAndThrowAsync(updateStatus);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(
                $"Bei der Aktualisierung des Status {status} mit dem Code {orderCode} ist ein Fehler aufgetreten: {ex.Message}");
            return false;
        }

        try
        {
            bool updateSuccess = await _orderRepository.UpdateOrderStatusAsync(orderCode, status);

            if (!updateSuccess)
            {
                _logger.LogError(
                    $"Fehler beim Aktualisieren des Status in der Datenbank für Bestellung mit Code {orderCode}.");
                return false;
            }

            _logger.LogInformation(
                $"Bestellung mit Code {orderCode} wurde erfolgreich in der Datenbank auf {status} aktualisiert.");
            return true;
        }
        catch (Exception ex)
        {
            var updateOrderStatusException = new UpdateOrderStatusException(orderCode, ex);
            _logger.LogError(updateOrderStatusException.Message);
            return false;
        }
    }
    
    public async Task<List<Order>> GetOrdersByStatusAsync(string status)
    {
        try
        {
            return await _orderRepository.GetOrdersWithStatusAsync(status);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von allen Bestellungen mit dem Status '{status}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von allen Bestellungen mit dem Status '{status}'.");
        }

        return new List<Order>();
    }
    
    public async Task UpdateOrderStatusByOrderCodeAsync(string orderCode, string newStatus)
    {
        try
        {
            await _orderRepository.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus);

            _logger.LogInformation(
                $"Der Status der Bestellung mit dem OrderCode '{orderCode}' wurde in der Datenbank erfolgreich auf '{newStatus}' aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{newStatus}' für die Bestellung mit dem OrderCode '{orderCode}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Status '{newStatus}' für die Bestellung mit dem OrderCode '{orderCode}'.");
        }
    }
    
    public async Task UpdateOrderStatusByIdAsync(int orderId, string status)
    {
        try
        {
            _logger.LogInformation(
                $"Es wird versucht den Status der Bestellung mit der Id '{orderId}' in der Datenbank auf '{status}' zu aktualisiert.");

            await _orderRepository.UpdateOrderStatusByIdAsync(orderId, status);

            _logger.LogInformation(
                $"Der Status der Bestellung mit der Id '{orderId}' wurde in der Datenbank erfolgreich auf '{status}' aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{status}' für die Bestellung mit der Id '{orderId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die Bestellung mit der Id '{orderId}'.");
            throw new OrderServiceException(
                $"Unerwarteter Fehler beim aktualisieren des Status '{status}' für die Bestellung mit der Id '{orderId}'.",
                ex);
        }
    }
    
    public async Task<List<Order>> GetAllOrdersByStatusAsync(string status)
    {
        if (string.IsNullOrEmpty(status))
        {
            _logger.LogError($"'{nameof(status)}' darf nicht null oder leer sein.");
            throw new ArgumentException($"'{nameof(status)}' darf nicht null oder leer sein.", nameof(status));
        }

        try
        {
            return await _orderRepository.GetOrdersWithStatusAsync(status);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von allen Bestellungen mit dem Status '{status}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von allen Bestellungen mit dem Status '{status}'.");
            throw new OrderServiceException(
                $"Unerwarteter Fehler beim Abrufen von allen Bestellungen mit dem Status '{status}'.", ex);
        }
    }
    
    public async Task<Order> GetOrderByIdAsync(int orderId)
    {
        if (orderId <= 0)
        {
            _logger.LogError($"'{nameof(orderId)}' muss größer als 0 sein.");
            throw new ArgumentException($"'{nameof(orderId)}' muss größer als 0 sein.");
        }

        Order? order = null;
        try
        {
            order = await _orderRepository.GetOrderByIdAsync(orderId);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex, $"Repository-Exception beim Abrufen von Bestellung mit der Id '{orderId}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unerwarteter Fehler beim Abrufen von Bestellung mit der Id '{orderId}'.");
            throw new OrderServiceException(
                $"Unerwarteter Fehler beim Abrufen von Bestellung mit der Id '{orderId}'.", ex);
        }

        if (order == null)
        {
            _logger.LogError($"{nameof(order)} mit ID {orderId} ist null.");
            throw new OrderIsNullException($"Keine Bestellung mit ID {orderId} gefunden.");
        }

        return order;
    }
    
    public async Task<List<Order>> SearchOrdersAsync(SearchTerm searchTerm, string status)
    {
        if (string.IsNullOrWhiteSpace(searchTerm.value))
        {
            return new List<Order>();
        }

        if (string.IsNullOrEmpty(status))
        {
            _logger.LogError($"'{nameof(status)}' darf nicht null sein.");
            throw new ArgumentNullException(nameof(status));
        }

        try
        {
            await _searchTermValidator.ValidateAndThrowAsync(searchTerm);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex,
                $"Es ist ein Validierungsfehler aufgetreten beim suchen von Order mit dem searchTerm '{searchTerm.value}'.");
            throw;
        }

        try
        {
            return await _orderRepository.SearchOrdersAsync(searchTerm, status);
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Suchen von stornierten Bestellungen mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Suchen von stornierten Bestellungen mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.");
            throw new OrderServiceException(
                $"Unerwarteter Fehler beim Suchen von stornierten Bestellungen mit dem Suchbegriff '{searchTerm.value}' und Status '{status}'.",
                ex);
        }
    }
    
    public async Task<bool> ProcessOrderEntriesCancellationAsync(int orderId, string orderCode, Dictionary<int, CancelOrderEntryModel> cancelledEntries)
    {
        ValidateInputs(orderId, orderCode, cancelledEntries);

        var order = await GetOrderByIdAsync(orderId);
        await ValidateCancellationEntriesAsync(cancelledEntries); 
        ProcessCancellationsAsync(order, cancelledEntries);

        var cancellationRequests = cancelledEntries
            .Where(e => e.Value.IsCancelled)
            .Select(e => new OrderCancellationEntry
            {
                orderEntryNumber = e.Key,
                cancelQuantity = e.Value.CancelQuantity,
                cancelReason = SharedStatus.CustomerRequest,
                notes = "Wurde auf Kundenwunsch storniert."
            }).ToList();

        bool apiResult = await SendCancellationRequestsAsync(orderCode, cancellationRequests);
        if (apiResult)
        {
            await CancelOrderEntriesAsync(order, cancellationRequests);
            return true;
        }

        return false;
    }
    
    private void ValidateInputs(int orderId, string orderCode, Dictionary<int, CancelOrderEntryModel> cancelledEntries)
    {
        if (orderId <= 0)
        {
            throw new InvalidIdException($"{nameof(orderId)} muss größer als 0 sein.");
        }

        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new OrderCodeIsNullException($"{nameof(orderCode)} darf nicht null sein.");
        }

        if (!cancelledEntries.Any(entry => entry.Value.IsCancelled))
        {
            var failures = new List<ValidationFailure>
            {
                new ValidationFailure("cancelledEntries", "Bitte markieren Sie mindestens eine Position.")
            };
            throw new ValidationException(new ValidationResult(failures).Errors);
        }
    }
    
    private async Task ValidateCancellationEntriesAsync(Dictionary<int, CancelOrderEntryModel> cancelledEntries)
    {
        foreach (var cancelEntry in cancelledEntries.Where(e => e.Value.IsCancelled))
        {
            await _cancelOrderEntryValidator.ValidateAndThrowAsync(cancelEntry.Value);
        }
    }
    
    private void ProcessCancellationsAsync(Order order, Dictionary<int, CancelOrderEntryModel> cancelledEntries)
    {
        foreach (var cancelEntry in cancelledEntries.Where(e => e.Value.IsCancelled))
        {
            var orderEntry = order.Entries.FirstOrDefault(ce => ce.Id == cancelEntry.Value.OrderEntryId);
            if (orderEntry != null && _quantityCheckService.IsQuantityExceedingAvailable(
                    orderEntry.CanceledOrReturnedQuantity, cancelEntry.Value.CancelQuantity, orderEntry.Quantity))
            {
                throw new QuantityExceededException("Die stornierte Menge überschreitet die verfügbare Menge.");
            }
        }
    }
    
    private async Task<bool> SendCancellationRequestsAsync(string orderCode, List<OrderCancellationEntry> cancellationRequests)
    {
        bool apiResult = await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationRequests);
        return apiResult;
    }
    
    private async Task CancelOrderEntriesAsync(Order order, List<OrderCancellationEntry> cancellationEntries)
    {
        foreach (var cancellationEntry in cancellationEntries)
        {
            var entry = order.Entries.FirstOrDefault(e => e.EntryNumber == cancellationEntry.orderEntryNumber);

            if (entry != null)
            {
                await _cancellationService.ProcessCancellationEntry(order, entry, cancellationEntry);
            }
        }

        if (_cancellationService.AreAllOrderEntriesCancelled(order))
        {
            await _cancellationService.CancelWholeOrder(order);
        }
    }
    
    public async Task<List<Order>> GetOrdersByIds(List<int> selectedOrders)
    {
        List<Order> orders = new List<Order>();

        foreach (var orderId in selectedOrders)
        {
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);

                if (order != null)
                {
                    orders.Add(order);
                }
                else
                {
                    _logger.LogWarning($"Es wurde keine Order mit der Id '{orderId}' gefunden.");
                }
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex,
                    $"Repository-Exception beim abrufen der Order mit der Id '{orderId}'.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Unerwarteter Fehler beim abrufen der Order mit der Id '{orderId}'.");
                throw new OrderServiceException(
                    $"Unerwarteter Fehler beim abrufen der Order mit der Id '{orderId}'.",
                    ex);
            }
        }

        return orders;
    }
    
    public async Task UpdateOrderExportedValue(List<Order> orders, bool exported)
    {
        foreach (var order in orders)
        {
            try
            {
                order.Exported = exported;
                await _orderRepository.UpdateOrderAsync(order);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex,
                    $"Repository-Exception beim aktualisieren der Order mit der Id '{order.Id}'.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Unerwarteter Fehler beim aktualisieren der Order mit der Id '{order.Id}'.");
                throw new OrderServiceException(
                    $"Unerwarteter Fehler beim aktualisieren der Order mit der Id '{order.Id}'.",
                    ex);
            }
        }
    }
}

