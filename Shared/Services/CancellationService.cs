using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class CancellationService : ICancellationService
{
    private readonly ILogger<CancellationService> _logger;
    private readonly IOrderRepository _orderRepository;
    private readonly IValidatorWrapper<ProcessCancellationEntry> _processCancellationEntryValidator;


    public CancellationService(ILogger<CancellationService> logger, IOrderRepository orderRepository,
        IValidatorWrapper<ProcessCancellationEntry> processCancellationEntryValidator)
    {
        _logger = logger;
        _orderRepository = orderRepository;
        _processCancellationEntryValidator = processCancellationEntryValidator;
    }
    
    public async Task ProcessCancellationEntry(Order order, OrderEntry orderEntry, OrderCancellationEntry cancellationEntry)
        {
            var processCancellationEntry = new ProcessCancellationEntry
            {
                Order = order,
                OrderEntry = orderEntry,
                OrderCancellationEntry = cancellationEntry
            };
            try
            {
              await _processCancellationEntryValidator.ValidateAndThrowAsync(processCancellationEntry);
                
            } catch (ValidationException ex)
            {
                _logger.LogError("Validierungsfehler bei der Verarbeitung einer Stornierung: {Errors}", ex.Errors);
                throw new ValidationException(ex.Errors);
            }
            

            if (cancellationEntry.cancelQuantity >= orderEntry.Quantity)
            {
                orderEntry.CanceledOrReturnedQuantity = orderEntry.Quantity; // Stornierung der kompletten Position
            }
            else
            {
                orderEntry.CanceledOrReturnedQuantity += cancellationEntry.cancelQuantity; // Erhöhen der Menge der Position
            }

            try
            {
                _logger.LogInformation(
                    $"Es wird versucht, Teile der Bestellung mit dem OrderCode {order.Code} zu stornieren.");
                await _orderRepository.UpdateOrderEntryAsync(orderEntry);
                _logger.LogInformation("Die Stornierung war erfolgreich.");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Repository-Exception beim Stornieren der Bestellung.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unerwarteter Fehler beim Stornieren von Teilen der Bestellung mit dem OrderCode '{order.Code}'.");
                throw new CancellationServiceException($"Fehler beim Stornieren von Teilen der Bestellung mit dem OrderCode '{order.Code}'.", ex);
            }
        }
    
        public bool AreAllOrderEntriesCancelled(Order order)
        {
            if (order == null)
            {
                _logger.LogError($"'{nameof(order)}' darf nicht null sein.");
                throw new OrderIsNullException(nameof(order));
            }

            if (order.Entries == null)
            {
                _logger.LogError($"'Entries' von '{nameof(order)}' darf nicht null sein.");
                throw new OrderEntryIsNullException($"Entries von {nameof(order)} sind null.");
            }

            return order.Entries.All(e => e.CanceledOrReturnedQuantity == e.Quantity);
        }
        
        public async Task CancelWholeOrder(Order order)
        {
            if (order == null)
            {
                _logger.LogError($"'{nameof(order)}' darf nicht null sein.");
                throw new OrderIsNullException(nameof(order));
            }

            order.Status = SharedStatus.Canceled;

            try
            {
                _logger.LogInformation($"Es wird versucht, die gesamte Bestellung mit dem OrderCode '{order.Code}' zu stornieren.");
                await _orderRepository.UpdateOrderAsync(order);
                _logger.LogInformation("Die Stornierung war erfolgreich.");
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Repository-Exception beim Stornieren der gesamten Bestellung.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unerwarteter Fehler beim Stornieren der gesamten Bestellung mit dem OrderCode '{order.Code}'.");
                throw new CancellationServiceException($"Fehler beim Stornieren der gesamten Bestellung mit dem OrderCode '{order.Code}'.", ex);
            }
        }
}