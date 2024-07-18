using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;

namespace Shared.Services;

public class DpdTrackingDataService : IDpdTrackingDataService
{
    private readonly ILogger<DpdTrackingDataService> _logger;
    private readonly IShippedOrdersProcessingService _shippedOrdersProcessingService;
    private readonly IConsignmentService _consignmentService;
    private readonly IOAuthClientService _oAuthClientService;

    public DpdTrackingDataService(ILogger<DpdTrackingDataService> logger,
        IShippedOrdersProcessingService shippedOrdersProcessingService,
        IConsignmentService consignmentService, IOAuthClientService oAuthClientService)
    {
        _logger = logger;
        _shippedOrdersProcessingService = shippedOrdersProcessingService;
        _consignmentService = consignmentService;
        _oAuthClientService = oAuthClientService;
    }

    public async Task ProcessTrackingData(TrackingData trackingData)
    {
        if (string.IsNullOrEmpty(trackingData.pnr))
        {
            throw new ArgumentNullException(nameof(trackingData.pnr),
                "Die Tracking-ID (pnr) darf nicht null oder leer sein.");
        }

        if (string.IsNullOrEmpty(trackingData.status))
        {
            throw new ArgumentNullException(nameof(trackingData.pnr), "Der Status darf nicht null oder leer sein.");
        }

        Consignment? consignmentFromDb =
            await _consignmentService.GetShippedConsignmentByTrackingIdAsync(trackingData.pnr);

        //Wenn die Trackingnummer nicht in der Datenbank vorhanden ist, soll sie einfach ignoriert werden
        if (consignmentFromDb == null)
        {
            return;
        }

        bool updateConsignmentResult =
            await _consignmentService.UpdateDpdConsignmentStatusAsync(trackingData.status, trackingData.pnr);

        //Update in DB erfolgreich -> API den status melden
        if (updateConsignmentResult)
        {
            try
            {
                if (trackingData.status.Equals(SharedStatus.delivery_customer) ||
                    trackingData.status.Equals(SharedStatus.pickup_by_consignee))
                {
                    await _oAuthClientService.ReportConsignmentDeliveryAsync(consignmentFromDb);
                }

                await _shippedOrdersProcessingService.CheckAndProcessShippedOrders();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Ein Fehler trat auf beim Aktualisieren des Consignments: {ex.Message}");
            }
        }
    }
}