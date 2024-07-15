using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;

namespace Shared.Services;

public class DhlTrackingStatusService : IDhlTrackingStatusService
{
    private readonly ILogger<DhlTrackingStatusService> _logger;
    private readonly IDhlApiService _dhlApiService;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly IConsignmentService _consignmentService;

    public DhlTrackingStatusService(ILogger<DhlTrackingStatusService> logger, IDhlApiService dhlApiService,
        IAccessTokenService accessTokenService, IOAuthClientService oAuthClientService,
        IConsignmentService consignmentService)
    {
        _logger = logger;
        _dhlApiService = dhlApiService;
        _accessTokenService = accessTokenService;
        _oAuthClientService = oAuthClientService;
        _consignmentService = consignmentService;
    }

    public async Task ReadAndUpdateTrackingStatusAsync()
    {
        await _accessTokenService.EnsureTokenDataExists();

        // Alle Consignments mit Status SHIPPED auslesen und prüfen ob die trackingnummer delivered oder failure ist
        var consignments = await _consignmentService.GetConsignmentsWithStatusShippedAsync();

        if (consignments.Count == 0)
        {
            _logger.LogInformation(
                $"Es wurden keine Lieferungen mit dem Status '{SharedStatus.Shipped}' in der Datenbank gefunden.");
            return;
        }

        foreach (var consignment in consignments)
        {
            // Zu viele Anfragen in kurzer Zeit an DHL API verhindern
            await Task.Delay(8000);

            string trackingCode = consignment.TrackingId;
            var status = await _dhlApiService.GetTrackingStatusFromApiAsync(trackingCode);

            bool updateConsignmentResult =
                await _consignmentService.UpdateConsignmentStatusByConsignmentIdAsync(status, consignment.Id);

            // Update in DB erfolgreich -> API den Status melden
            if (updateConsignmentResult)
            {
                var consignmentFromDb = await _consignmentService.GetConsignmentByConsignmentIdAsync(consignment.Id);

                if (consignmentFromDb != null && status.Equals(SharedStatus.Delivered))
                {
                    await _oAuthClientService.ReportConsignmentDeliveryAsync(consignmentFromDb);
                }
            }
        }
    }
}