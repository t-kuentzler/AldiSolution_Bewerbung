using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class OAuthClientService : IOAuthClientService
    {
        private readonly OAuthSettings _settings;
        private readonly IAccessTokenService _accessTokenService;
        private readonly ILogger<OAuthClientService> _logger;
        private readonly IHttpClientFactory _clientFactory;


        public OAuthClientService(IOptions<OAuthSettings> settings, IAccessTokenService accessTokenService,
            ILogger<OAuthClientService> logger, IHttpClientFactory clientFactory)
        {
            _settings = settings.Value;
            _accessTokenService = accessTokenService;
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public async Task<OAuthTokenResponse?> GetApiTokenAsync()
        {
            var client = _clientFactory.CreateClient();

            // Erstellen des Authorization-Headers für Basic Auth
            var authHeaderValue =
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.VendorId}:{_settings.Secret}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authHeaderValue);

            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", _settings.VendorId),
                new KeyValuePair<string, string>("password", _settings.Password)
            };

            var content = new FormUrlEncodedContent(postData);

            string tokenUrl = $"{_settings.BaseUrl}authorizationserver/oauth/token";

            try
            {
                var response = await client.PostAsync(tokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    OAuthTokenResponse? tokenResponse =
                        JsonConvert.DeserializeObject<OAuthTokenResponse>(jsonContent);

                    return tokenResponse;
                }
                else
                {
                    _logger.LogError($"Failed to obtain token. Status Code: {response.StatusCode}");
                    _logger.LogError($"Response: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP-Anfragefehler beim Abrufen des Tokens von der API.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler beim Abrufen des Tokens von der API.");
            }

            return null;
        }


        public async Task<OrderResponse> GetApiOrdersAsync()
        {
            _logger.LogInformation("Es wird versucht die Bestellungen abzurufen.");

            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();
            using (var client = _clientFactory.CreateClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

                string ordersUrl = $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/magmaheimtex_DE/orders?pageNumber=0&pageSize=100";

                try
                {
                    var response = await client.GetAsync(ordersUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        OrderResponse? orderResponse = JsonConvert.DeserializeObject<OrderResponse>(jsonContent);

                        if (orderResponse?.Orders != null)
                        {
                            _logger.LogInformation($"Es wurden {orderResponse.Orders.Count} Bestellungen abgerufen.");
                            return orderResponse;
                        }
                        else
                        {
                            return new OrderResponse();
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Der Token ist ungültig. Es wird versucht ihn zu aktualisieren.");
                        var newToken = await _accessTokenService.GetAndUpdateNewAccessToken();
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                        response = await client.GetAsync(ordersUrl);

                        if (response.IsSuccessStatusCode)
                        {
                            var jsonContent = await response.Content.ReadAsStringAsync();
                            OrderResponse? orderResponse = JsonConvert.DeserializeObject<OrderResponse>(jsonContent);

                            if (orderResponse?.Orders != null)
                            {
                                _logger.LogInformation(
                                    $"Es wurden {orderResponse.Orders.Count} Bestellungen abgerufen.");
                                return orderResponse;
                            }
                            else
                            {
                                _logger.LogInformation("Es wurden 0 Bestellungen abgerufen.");
                                return new OrderResponse();
                            }
                        }
                        else
                        {
                            _logger.LogError(
                                $"Could not obtain orders. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            $"Could not obtain orders. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP-Anfragefehler beim Abrufen der Bestellungen von der API.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unerwarteter Fehler beim Abrufen der Bestellungen von der API.");
                }
            }

            return new OrderResponse();
        }


        public async Task<bool> CancelOrderEntriesAsync(string orderCode,
            IEnumerable<OrderCancellationEntry> cancellationEntries)
        {
            const int maxRetryCount = 1;
            int retryCount = 0;

            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    using (var client = _clientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenResponse);

                        string cancellationUrl =
                            $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/orders/{orderCode}/cancellations";

                        var jsonContent = JsonConvert.SerializeObject(cancellationEntries);

                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(cancellationUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation(
                                $"API-Stornierung Bestellung mit dem OrderCode '{orderCode}' war erfolgreich.");
                            return true;
                        }

                        if (response.StatusCode == HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                        {
                            _logger.LogWarning(
                                "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                            tokenResponse = await _accessTokenService.GetAndUpdateNewAccessToken();

                            retryCount++;
                        }
                        else
                        {
                            _logger.LogError(
                                $"Es ist ein Fehler bei der Stornierung aufgetreten. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                            return false;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        $"HTTP-Anfragefehler bei API-Stornierung von Bestellung mit dem OrderCode '{orderCode}'.");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler bei API-Stornierung von Bestellung mit dem OrderCode '{orderCode}'.");
                    return false;
                }
            }

            _logger.LogError(
                $"Maximale Anzahl an Wiederholungsversuchen ({maxRetryCount}) erreicht. API-Stornierung von Bestellung mit dem OrderCode '{orderCode}' fehlgeschlagen.");
            return false;
        }


        public async Task<(bool, ManualReturnResponse)> CreateManualReturnAsync(ManualReturnRequest manualReturnRequest)
        {
            const int maxRetryCount = 1;
            int retryCount = 0;
            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    using (var client = _clientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenResponse);
                        string returnUrl =
                            $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/returns/";

                        var jsonContent = JsonConvert.SerializeObject(manualReturnRequest);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PostAsync(returnUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var returnResponse = JsonConvert.DeserializeObject<ManualReturnResponse>(responseContent);

                            _logger.LogInformation(
                                $"Manuelle Retoure für Bestellung mit dem OrderCode '{manualReturnRequest.orderCode}' erfolgreich erstellt.");

                            if (returnResponse == null)
                            {
                                return (false, new ManualReturnResponse());
                            }

                            return (true, returnResponse);
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                        {
                            _logger.LogWarning(
                                "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                            tokenResponse = await _accessTokenService.GetAndUpdateNewAccessToken();

                            retryCount++;
                        }
                        else
                        {
                            _logger.LogError(
                                $"Fehler bei der Erstellung der manuellen Retoure mit dem OrderCode '{manualReturnRequest.orderCode}'. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                            return (false, new ManualReturnResponse());
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        $"HTTP-Anfragefehler bei der Erstellung der manuellen Retoure für Bestellung mit dem OrderCode '{manualReturnRequest.orderCode}'.");
                    return (false, new ManualReturnResponse());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler bei der Erstellung der manuellen Retoure für Bestellung mit dem OrderCode '{manualReturnRequest.orderCode}'.");
                    return (false, new ManualReturnResponse());
                }
            }

            _logger.LogError(
                $"Maximale Anzahl an Wiederholungsversuchen ({maxRetryCount}) erreicht. Erstellung der manuellen Retoure für Bestellung mit dem OrderCode '{manualReturnRequest.orderCode}' fehlgeschlagen.");
            return (false, new ManualReturnResponse());
        }


        public async Task<(bool, ReceivingReturnResponse)> CreateReceivingReturn(
            ReceivingReturnRequest parsedReceivingReturnRequest)
        {
            const int maxRetryCount = 1;
            int retryCount = 0;
            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    using (var client = _clientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenResponse);
                        string returnUrl =
                            $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/returns/";

                        var jsonSettings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        var jsonContent = JsonConvert.SerializeObject(parsedReceivingReturnRequest, jsonSettings);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PutAsync(returnUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync();
                            var returnResponse =
                                JsonConvert.DeserializeObject<ReceivingReturnResponse>(responseContent);

                            _logger.LogInformation(
                                $"Retoure mit dem AldiReturnCode '{parsedReceivingReturnRequest.aldiReturnCode}' erfolgreich über die API aktualisiert.");

                            if (returnResponse == null)
                            {
                                return (false, new ReceivingReturnResponse());
                            }

                            return (true, returnResponse);
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                        {
                            _logger.LogWarning(
                                "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                            tokenResponse = await _accessTokenService.GetAndUpdateNewAccessToken();

                            retryCount++;
                        }
                        else
                        {
                            _logger.LogError(
                                $"Fehler bei der Aktualisierung der Retoure mit dem AldiReturnCode '{parsedReceivingReturnRequest.aldiReturnCode}'. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                            return (false, new ReceivingReturnResponse());
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        $"HTTP-Anfragefehler bei der Aktualisierung der Retoure mit dem AldiReturnCode '{parsedReceivingReturnRequest.aldiReturnCode}'.");
                    return (false, new ReceivingReturnResponse());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler bei der Aktualisierung der Retoure mit dem AldiReturnCode '{parsedReceivingReturnRequest.aldiReturnCode}' über die API.");
                    return (false, new ReceivingReturnResponse());
                }
            }

            _logger.LogError(
                $"Maximale Anzahl an Wiederholungsversuchen ({maxRetryCount}) erreicht. Aktualisierung der Retoure mit dem AldiReturnCode '{parsedReceivingReturnRequest.aldiReturnCode}' über die API fehlgeschlagen.");
            return (false, new ReceivingReturnResponse());
        }

        public async Task<bool> CancelConsignmentAfterDispatchAsync(Consignment consignment)
        {
            const int maxRetryCount = 1;
            int retryCount = 0;

            _logger.LogInformation($"Es wird versucht, das Consignment mit der Id {consignment.Id} zu stornieren.");

            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    using (var client = _clientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenResponse);

                        // Vorbereitung der Daten für den Stornierungs-Request
                        var cancellationEntries = consignment.ConsignmentEntries.Select(e => new
                        {
                            orderEntryNumber = e.OrderEntryNumber,
                            quantity = e.Quantity
                        }).ToArray();

                        var cancellationReport = new
                        {
                            carrier = consignment.Carrier,
                            entries = cancellationEntries,
                            shippingAddress = new
                            {
                                countryIsoCode = consignment.ShippingAddress.CountryIsoCode,
                                type = consignment.ShippingAddress.Type
                            },
                            status = SharedStatus.Cancelled,
                            statusText = "Storniert",
                            trackingId = consignment.TrackingId,
                            trackingLink =
                                $"https://www.dhl.de/de/privatkunden/dhl-sendungsverfolgung.html?piececode={consignment.TrackingId}",
                            vendorConsignmentCode = consignment.VendorConsignmentCode
                        };

                        var jsonRequest = JsonConvert.SerializeObject(cancellationReport);
                        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                        // URL-Konstruktion basierend auf den Einstellungen und dem VendorConsignmentCode
                        string url =
                            $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/orders/{consignment.OrderCode}/consignments/{consignment.AldiConsignmentCode}";

                        var response = await client.PutAsync(url, content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation(
                                $"Stornierung des Consignments mit der Id '{consignment.Id}' war erfolgreich.");
                            return true;
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                        {
                            _logger.LogWarning(
                                "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                            tokenResponse = await _accessTokenService.GetAndUpdateNewAccessToken();

                            retryCount++;
                        }
                        else
                        {
                            _logger.LogError(
                                $"Fehler bei der Stornierung des Consignments mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}'. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                            return false;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        $"HTTP-Anfragefehler bei der Stornierung des Consignments mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}'.");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler bei der Stornierung des Consignments mit dem VendorConsignmentCode '{consignment.VendorConsignmentCode}' über die API.");
                    return false;
                }
            }

            _logger.LogError(
                $"Maximale Anzahl an Wiederholungsversuchen ({maxRetryCount}) erreicht. Stornierung des Consignments mit der Id '{consignment.Id}' über die API fehlgeschlagen.");
            return false;
        }

        public async Task<bool> ReportReturnPackage(ReportReturnPackageRequest reportReturnPackageRequest)
        {
            const int maxRetryCount = 1;
            int retryCount = 0;

            var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();

            while (retryCount <= maxRetryCount)
            {
                try
                {
                    using (var client = _clientFactory.CreateClient())
                    {
                        client.DefaultRequestHeaders.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenResponse);
                        string returnUrl =
                            $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/returns/";

                        var jsonSettings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        var jsonContent = JsonConvert.SerializeObject(reportReturnPackageRequest, jsonSettings);
                        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await client.PutAsync(returnUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInformation(
                                $"Status des ReturnPackages mit dem AldiReturnCode '{reportReturnPackageRequest.aldiReturnCode}' erfolgreich über die API aktualisiert.");
                            return true;
                        }
                        else if (response.StatusCode == HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                        {
                            _logger.LogWarning(
                                "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                            tokenResponse = await _accessTokenService.GetAndUpdateNewAccessToken();

                            retryCount++;
                        }
                        else
                        {
                            _logger.LogError(
                                $"Fehler bei der Aktualisierung des ReturnPackages mit dem AldiReturnCode '{reportReturnPackageRequest.aldiReturnCode}'. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                            return false;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex,
                        $"HTTP-Anfragefehler bei der Aktualisierung des ReturnPackages mit dem AldiReturnCode '{reportReturnPackageRequest.aldiReturnCode}'.");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler bei der Aktualisierung des ReturnPackages mit dem AldiReturnCode '{reportReturnPackageRequest.aldiReturnCode}' über die API.");
                    return false;
                }
            }

            _logger.LogError(
                $"Maximale Anzahl an Wiederholungsversuchen ({maxRetryCount}) erreicht. Aktualisierung des ReturnPackages mit dem AldiReturnCode '{reportReturnPackageRequest.aldiReturnCode}' über die API fehlgeschlagen.");
            return false;
        }
        
        public async Task<bool> UpdateApiOrderStatusInProgressAsync(Order? order, int retryCount = 0)
        {
            const int maxRetryCount = 5;
            const int delayDuration = 3000;

            if (order == null)
            {
                _logger.LogError("Die übergebene Bestellung ist null.");
                return false;
            }

            _logger.LogInformation(
                $"Versuch, den API Status der Order mit dem OrderCode '{order.Code}' auf IN PROGRESS zu aktualisieren.");

            await Task.Delay(delayDuration);

            using (var client = _clientFactory.CreateClient())
            {
                var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

                string updateStatusUrl =
                    $"{_settings.BaseUrl}/aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/orders/{order.Code}";
                var updateContent = new StringContent(JsonConvert.SerializeObject(new { inProgress = true }),
                    Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PutAsync(updateStatusUrl, updateContent);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        $"Bestellung mit Code {order.Code} wurde in der API erfolgreich auf IN PROGRESS aktualisiert.");
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && retryCount < maxRetryCount)
                {
                    _logger.LogWarning(
                        "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");

                    await _accessTokenService.GetAndUpdateNewAccessToken();
                    return await UpdateApiOrderStatusInProgressAsync(order, retryCount + 1);
                }
                else
                {
                    _logger.LogError(
                        $"Fehler beim Aktualisieren des API Status für Bestellung mit Code {order.Code}. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                    return false;
                }
            }
        }
        
        public async Task<ConsignmentListResponse> CreateApiConsignmentAsync(
            List<ConsignmentRequest> consignmentRequestsList, string orderCode, int retryCount = 0)
        {
            const int maxRetryCount = 2;
            const int delayDuration = 3000;

            _logger.LogInformation(
                $"Versuch, ein Consignment über die Aldi API zu erstellen für VendorConsignmentCode '{consignmentRequestsList.FirstOrDefault()?.vendorConsignmentCode}'.");

            await Task.Delay(
                delayDuration); // Füge eine Verzögerung von 3 Sekunden hinzu, bevor die Anfrage gestartet wird

            using (var client = _clientFactory.CreateClient())
            {
                var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

                var jsonRequest = JsonConvert.SerializeObject(consignmentRequestsList);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                string consignmentUrl =
                    $"{_settings.BaseUrl}aldivendorwebservices/2.0/DE/vendor/{_settings.VendorId}/orders/{orderCode}/consignments";

                try
                {
                    var response = await client.PostAsync(consignmentUrl, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = JsonConvert.DeserializeObject<ConsignmentListResponse>(responseBody);
                        _logger.LogInformation(
                            $"Consignment für VendorConsignmentCode '{consignmentRequestsList.FirstOrDefault()?.vendorConsignmentCode}' wurde erfolgreich über API erstellt.");
                        return responseJson ??
                               throw new ConsignmentResponseIsNullException("ConsignmentResponse der API ist null.");
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                             retryCount < maxRetryCount)
                    {
                        _logger.LogWarning(
                            "Der Token ist ungültig. Es wird versucht, ihn zu aktualisieren und die Anfrage zu wiederholen.");
                        await _accessTokenService.GetAndUpdateNewAccessToken();
                        return await CreateApiConsignmentAsync(consignmentRequestsList, orderCode, retryCount + 1);
                    }
                    else
                    {
                        _logger.LogError(
                            $"Fehler beim Erstellen des Consignment für VendorConsignmentCode '{consignmentRequestsList.FirstOrDefault()?.vendorConsignmentCode}' über die Aldi API. Status Code: {response.StatusCode}, Response: {responseBody}");
                        throw new ApiException(
                            $"API-Fehler beim Erstellen des Consignment: Status Code {response.StatusCode}, Response: {responseBody}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, $"HTTP-Anfragefehler bei Erstellung des Consignment.");
                    throw new ApiException(
                        $"HTTP-Anfragefehler bei der Kommunikation mit der API, Message: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unerwarteter Fehler bei Erstellung des Consignment.");
                    throw new ApiException(
                        $"Unerwarteter Fehler bei der Verarbeitung der Anfrage, Message: {ex.Message}", ex);
                }
            }
        }
        
        public async Task<ReturnResponse> GetApiReturnsWithStatusCreatedAsync(string status)
        {
            _logger.LogInformation("Es wird versucht die Retouren abzurufen.");
            using (var client = new HttpClient())
            {
                var tokenResponse = await _accessTokenService.ValidateAndGetAccessToken();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

                string returnUrl = $"{_settings.BaseUrl}{_settings.GetReturnsEndpoint}";
                var response = await client.GetAsync(returnUrl);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    ReturnResponse? returnResponse = JsonConvert.DeserializeObject<ReturnResponse>(jsonContent);

                    if (returnResponse?.ReturnRequests.Count != null)
                    {
                        _logger.LogInformation($"Es wurden {returnResponse?.ReturnRequests.Count} Retouren abgerufen.");
                        return returnResponse;
                    }
                    else
                    {
                        _logger.LogInformation("Es wurden 0 Retouren abgerufen.");
                        return new ReturnResponse();
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Der Token ist ungültig. Es wird versucht ihn zu aktualisieren.");
                    var newToken = await _accessTokenService.GetAndUpdateNewAccessToken();

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    response = await client.GetAsync(returnUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        ReturnResponse? returnResponse = JsonConvert.DeserializeObject<ReturnResponse>(jsonContent);

                        if (returnResponse?.ReturnRequests.Count != null)
                        {
                            _logger.LogInformation(
                                $"Es wurden {returnResponse?.ReturnRequests.Count} Retouren abgerufen.");
                            return returnResponse;
                        }
                        else
                        {
                            _logger.LogInformation("Es wurden 0 Retouren abgerufen.");
                            return new ReturnResponse();
                        }
                    }
                    else
                    {
                        _logger.LogError(
                            $"Could not obtain orders. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                        return new ReturnResponse();
                    }
                }
                else
                {
                    _logger.LogError(
                        $"Could not obtain orders. Status Code: {response.StatusCode}, Response: {await response.Content.ReadAsStringAsync()}");
                    return new ReturnResponse();
                }
            }
        }
    }
