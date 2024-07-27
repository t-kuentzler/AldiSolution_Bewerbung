using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Shared.Services
{
    public class OrderProcessingService : IOrderProcessingService
    {
        private readonly IAccessTokenService _accessTokenService;
        private readonly IOAuthClientService _oAuthClientService;
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderProcessingService> _logger;

        public OrderProcessingService(
            IAccessTokenService accessTokenService,
            IOAuthClientService oAuthClientService,
            IOrderService orderService,
            ILogger<OrderProcessingService> logger)
        {
            _accessTokenService = accessTokenService;
            _oAuthClientService = oAuthClientService;
            _orderService = orderService;
            _logger = logger;
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
                    await _orderService.ProcessSingleOrderAsync(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ein Fehler ist beim Abrufen der offenen Bestellungen aufgetreten.");
            }
        }
    }
}