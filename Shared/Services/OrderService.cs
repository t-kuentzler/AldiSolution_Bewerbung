using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class OrderService : IOrderService
{
    private readonly IAccessTokenService _accessTokenService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly ILogger<OrderService> _logger;
    private readonly IValidatorWrapper<Order> _orderValidator;
    private readonly IValidatorWrapper<UpdateStatus> _updateStatusValidator;
    private readonly IOrderRepository _orderRepository;


    public OrderService(IAccessTokenService accessTokenService, IOAuthClientService oAuthClientService,
        ILogger<OrderService> logger, IValidatorWrapper<Order> orderValidator,
        IValidatorWrapper<UpdateStatus> updateStatusValidator, IOrderRepository orderRepository)
    {
        _accessTokenService = accessTokenService;
        _oAuthClientService = oAuthClientService;
        _logger = logger;
        _orderValidator = orderValidator;
        _updateStatusValidator = updateStatusValidator;
        _orderRepository = orderRepository;
    }

    public async Task ProcessOpenOrdersAsync()
    {
        try
        {
            await _accessTokenService.EnsureTokenDataExists();
            var orders = await _oAuthClientService.GetApiOrdersAsync();

            if (orders.Orders.Count == 0)
            {
                _logger.LogInformation("Es sind keine offenen Bestellungen zum Abrufen verfügbar.");
                return;
            }

            foreach (var order in orders.Orders)
            {
                await ProcessSingleOrderAsync(order);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ein Fehler ist beim Abrufen der offenen Bestellungen aufgetreten.");
        }
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
}

