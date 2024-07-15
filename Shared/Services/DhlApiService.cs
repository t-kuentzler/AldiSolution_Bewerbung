using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Contracts;
using Shared.Models;

namespace Shared.Services;

public class DhlApiService : IDhlApiService
{
    private readonly DhlSettings _dhlSettings;
    private readonly ILogger<DhlApiService> _logger;

    public DhlApiService(ILogger<DhlApiService> logger, IOptions<DhlSettings> dhlSettings)
    {
        _logger = logger;
        _dhlSettings = dhlSettings.Value;
    }

    public async Task<string> GetTrackingStatusFromApiAsync(string trackingNumber)
    {
        _logger.LogInformation($"Es wird versucht, Informationen für die Trackingnummer {trackingNumber} abzurufen");

        using (var client = new HttpClient())
        {
            string url = $"{_dhlSettings.BaseUrl}track/shipments?trackingNumber={trackingNumber}";
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            request.Headers.Add("DHL-API-Key", _dhlSettings.ApiKey);

            using (var response = await client.SendAsync(request))
            {
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        $"API-Antwort mit Fehlerstatuscode: {response.StatusCode}. Trackingnummer: {trackingNumber}");
                    return string.Empty;
                }

                var body = await response.Content.ReadAsStringAsync();
                var shipmentResponse = JsonConvert.DeserializeObject<ShipmentResponse>(body);

                if (shipmentResponse?.Shipments != null)
                {
                    var firstShipment = shipmentResponse.Shipments.FirstOrDefault();
                    if (firstShipment?.Status != null && !string.IsNullOrEmpty(firstShipment.Status.StatusCode))
                    {
                        _logger.LogInformation(
                            $"Für die Trackingnummer '{trackingNumber}' wurde der Status '{firstShipment.Status.StatusCode}' abgerufen.");
                        return firstShipment.Status.StatusCode;
                    }
                }

                _logger.LogWarning($"Keine Sendungsinformationen gefunden für Trackingnummer {trackingNumber}.");
                return string.Empty;
            }
        }
    }
}