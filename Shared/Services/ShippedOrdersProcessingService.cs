using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;

namespace Shared.Services;

public class ShippedOrdersProcessingService : IShippedOrdersProcessingService
{
    private readonly ILogger<ShippedOrdersProcessingService> _logger;
    private readonly IOrderService _orderService;
    public ShippedOrdersProcessingService(ILogger<ShippedOrdersProcessingService> logger, IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }
    
    public async Task CheckAndProcessShippedOrders()
    {
        try
        {
            var shippedOrders = await _orderService.GetOrdersByStatusAsync(SharedStatus.Shipped);

            foreach (var order in shippedOrders)
            {
                //Wenn eine Consignment zugestellt wurde, Order als DELIVERED aktualisieren
                if (order.Consignments.Any(consignment => consignment.Status == SharedStatus.Delivered))
                {
                    await _orderService.UpdateOrderStatusByOrderCodeAsync(order.Code, SharedStatus.Delivered);

                    //Wenn alle Consignments der Oder CANCELLED sind, Status der Order als CANCELLED aktualisieren
                }
                else if (order.Consignments.All(consignment => consignment.Status == SharedStatus.Cancelled))
                {
                    await _orderService.UpdateOrderStatusByOrderCodeAsync(order.Code, SharedStatus.Canceled);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Es ist ein unerwarteter Fehler aufgetreten.");
        }
    }
    
    
}