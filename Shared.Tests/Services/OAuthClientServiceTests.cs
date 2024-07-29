using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

public class OAuthClientServiceTests
{
    private readonly Mock<IAccessTokenService> _accessTokenServiceMock;
    private readonly Mock<ILogger<OAuthClientService>> _loggerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly OAuthClientService _oAuthClientService;
    private readonly IOptions<OAuthSettings> _settings;
    private readonly Consignment _testConsignment;

    public OAuthClientServiceTests()
    {
        _accessTokenServiceMock = new Mock<IAccessTokenService>();
        _loggerMock = new Mock<ILogger<OAuthClientService>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var settingsValue = new OAuthSettings
        {
            VendorId = "test_vendor_id",
            Password = "test_password",
            Secret = "test_secret",
            BaseUrl = "https://api.test.com"
            // TokenEndpoint = "/oauth/token",
            // GetOrdersEndpoint = "/orders"
        };
        _settings = Options.Create(settingsValue);

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri(settingsValue.BaseUrl)
        };

        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _oAuthClientService = new OAuthClientService(
            _settings,
            _accessTokenServiceMock.Object,
            _loggerMock.Object,
            _httpClientFactoryMock.Object
        );

        _testConsignment = new Consignment
        {
            Id = 1,
            ConsignmentEntries = new List<ConsignmentEntry>
            {
                new ConsignmentEntry { OrderEntryNumber = 1, Quantity = 2 }
            },
            Carrier = "DHL",
            ShippingAddress = new ShippingAddress() { CountryIsoCode = "DE", Type = "Shipping" },
            TrackingId = "TRACK123",
            VendorConsignmentCode = "VCC123",
            AldiConsignmentCode = "ACC123",
            OrderCode = "OC123"
        };
    }

    //GetApiTokenAsync
    [Fact]
    public async Task GetApiTokenAsync_Success()
    {
        // Arrange
        var tokenResponse = new OAuthTokenResponse
            { AccessToken = "test_access_token", TokenType = "Bearer", ExpiresIn = 3600 };
        var responseContent = JsonConvert.SerializeObject(tokenResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri.ToString().Contains("/oauth/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _oAuthClientService.GetApiTokenAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test_access_token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task GetApiTokenAsync_ApiFailure()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _oAuthClientService.GetApiTokenAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetApiTokenAsync_NetworkError()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error occurred"));

        // Act & Assert
        await _oAuthClientService.GetApiTokenAsync();
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetApiTokenAsync_InvalidJsonResponse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{invalid_json_response}", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        // Act
        var result = await _oAuthClientService.GetApiTokenAsync();

        // Assert
        Assert.Null(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetApiTokenAsync_Timeout()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The request was canceled due to timeout."));

        // Act & Assert
        await _oAuthClientService.GetApiTokenAsync();
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
    }

    //GetApiOrdersAsync
    [Fact]
    public async Task GetApiOrdersAsync_Success()
    {
        // Arrange
        var orders = new List<Order> { new Order { Id = 1 } };
        var orderResponse = new OrderResponse { Orders = orders };
        var responseContent = JsonConvert.SerializeObject(orderResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.GetApiOrdersAsync();

        // Assert
        Assert.NotNull(result.Orders);
        Assert.Equal(1, result?.Orders.Count);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetApiOrdersAsync_UnauthorizedAndRefreshToken()
    {
        // Arrange
        var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var orders = new List<Order> { new Order { Id = 1 } };
        var orderResponse = new OrderResponse { Orders = orders };
        var responseContent = JsonConvert.SerializeObject(orderResponse);
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
                callCount++ == 0
                    ? unauthorizedResponse
                    : successResponse); // Conditional response based on the call count

        _accessTokenServiceMock.SetupSequence(x => x.ValidateAndGetAccessToken())
            .ReturnsAsync("expired_token") // First call returns an expired token
            .ReturnsAsync("new_valid_token"); // Second call returns a new valid token

        _accessTokenServiceMock.Setup(x => x.GetAndUpdateNewAccessToken())
            .ReturnsAsync("new_valid_token"); // Refresh token method

        // Act
        var result = await _oAuthClientService.GetApiOrdersAsync();

        // Assert
        Assert.NotNull(result.Orders);
        Assert.Equal(1, result?.Orders.Count);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetApiOrdersAsync_HttpError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.GetApiOrdersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Orders);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetApiOrdersAsync_NetworkException()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error occurred"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        await _oAuthClientService.GetApiOrdersAsync();
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task GetApiOrdersAsync_Timeout()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(
                new TaskCanceledException("The operation was canceled due to timeout.")); // Simuliert ein Timeout

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        await _oAuthClientService.GetApiOrdersAsync();
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //CancelOrderEntriesAsync
    [Fact]
    public async Task CancelOrderEntriesAsync_Success()
    {
        // Arrange
        var orderCode = "12345";
        var cancellationEntries = new List<OrderCancellationEntry>
        {
            new OrderCancellationEntry
                { orderEntryNumber = 1, cancelQuantity = 1, cancelReason = "TEST", notes = "TEST" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationEntries);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelOrderEntriesAsync_FailureResponse()
    {
        // Arrange
        var orderCode = "12345";
        var cancellationEntries = new List<OrderCancellationEntry>
        {
            new OrderCancellationEntry
                { orderEntryNumber = 1, cancelQuantity = 1, cancelReason = "TEST", notes = "TEST" }
        };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationEntries);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelOrderEntriesAsync_NetworkException()
    {
        // Arrange
        var orderCode = "12345";
        var cancellationEntries = new List<OrderCancellationEntry>
        {
            new OrderCancellationEntry
                { orderEntryNumber = 1, cancelQuantity = 1, cancelReason = "TEST", notes = "TEST" }
        };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error occurred"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationEntries);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelOrderEntriesAsync_Exception()
    {
        // Arrange
        var orderCode = "12345";
        var cancellationEntries = new List<OrderCancellationEntry>
        {
            new OrderCancellationEntry
                { orderEntryNumber = 1, cancelQuantity = 1, cancelReason = "TEST", notes = "TEST" }
        };
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationEntries);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelOrderEntriesAsync_Timeout()
    {
        // Arrange
        var orderCode = "12345";
        var cancellationEntries = new List<OrderCancellationEntry>
        {
            new OrderCancellationEntry
                { orderEntryNumber = 1, cancelQuantity = 1, cancelReason = "TEST", notes = "TEST" }
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(
                new TaskCanceledException("The operation was canceled due to timeout.")); // Simuliert ein Timeout

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        await _oAuthClientService.CancelOrderEntriesAsync(orderCode, cancellationEntries);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //CreateManualReturnAsync
    [Fact]
    public async Task CreateManualReturnAsync_Success()
    {
        // Arrange
        var manualReturnRequest = new ManualReturnRequest { orderCode = "12345" };
        var manualReturnResponse = new ManualReturnResponse
            { orderCode = "12345", aldiReturnCode = "AR12345", rma = "RMA12345", initiationDate = DateTime.UtcNow };
        var responseContent = JsonConvert.SerializeObject(manualReturnResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, result) = await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal("12345", result.orderCode);
        Assert.Equal("AR12345", result.aldiReturnCode);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateManualReturnAsync_FailureStatusCode()
    {
        // Arrange
        var manualReturnRequest = new ManualReturnRequest { orderCode = "12345" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error processing request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, result) = await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        // Assert
        Assert.False(success);
        Assert.NotNull(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateManualReturnAsync_DeserializationFailure()
    {
        // Arrange
        var manualReturnRequest = new ManualReturnRequest { orderCode = "12345" };
        var responseContent = "invalid_json";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, result) = await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        // Assert
        Assert.False(success);
        Assert.NotNull(result); // result should still be initialized to an empty object
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateManualReturnAsync_ExceptionHandling()
    {
        // Arrange
        var manualReturnRequest = new ManualReturnRequest { orderCode = "12345" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, result) = await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        // Assert
        Assert.False(success);
        Assert.NotNull(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateManualReturnAsync_Timeout()
    {
        // Arrange
        var manualReturnRequest = new ManualReturnRequest { orderCode = "12345" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The operation was canceled due to a timeout."));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var (success, result) = await _oAuthClientService.CreateManualReturnAsync(manualReturnRequest);

        // Assert
        Assert.False(success);
        Assert.NotNull(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //CreateReceivingReturn
    [Fact]
    public async Task CreateReceivingReturn_Success()
    {
        // Arrange
        var request = new ReceivingReturnRequest
        {
            aldiReturnCode = "ARC123",
            orderCode = "OC12345",
            initiationDate = "2023-06-01",
            customerInfo = new ReceivingReturnCustomerInfoRequest(),
            entries = new List<ReceivingReturnEntriesRequest>()
        };
        var responseContent = JsonConvert.SerializeObject(new ReceivingReturnResponse());
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, returnResponse) = await _oAuthClientService.CreateReceivingReturn(request);

        // Assert
        Assert.True(success);
        Assert.NotNull(returnResponse);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReceivingReturn_FailureStatusCode()
    {
        // Arrange
        var request = new ReceivingReturnRequest { aldiReturnCode = "ARC123" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var (success, returnResponse) = await _oAuthClientService.CreateReceivingReturn(request);

        // Assert
        Assert.False(success);
        Assert.NotNull(returnResponse);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReceivingReturn_NetworkError()
    {
        // Arrange
        var request = new ReceivingReturnRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error occurred"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var (success, returnResponse) = await _oAuthClientService.CreateReceivingReturn(request);

        // Assert
        Assert.False(success);
        Assert.NotNull(returnResponse);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReceivingReturn_GeneralException()
    {
        // Arrange
        var request = new ReceivingReturnRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var (success, returnResponse) = await _oAuthClientService.CreateReceivingReturn(request);

        // Assert
        Assert.False(success);
        Assert.NotNull(returnResponse); // Sollte immer noch eine leere Antwortinstanz zurückgeben
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateReceivingReturn_Timeout()
    {
        // Arrange
        var request = new ReceivingReturnRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(
                new TaskCanceledException("The operation was canceled due to a timeout.")); // Simuliert ein Timeout

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var (success, returnResponse) = await _oAuthClientService.CreateReceivingReturn(request);

        // Assert
        Assert.False(success);
        Assert.NotNull(returnResponse);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //CancelConsignmentAfterDispatchAsync
    [Fact]
    public async Task CancelConsignmentAfterDispatchAsync_Success()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.CancelConsignmentAfterDispatchAsync(_testConsignment);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task CancelConsignmentAfterDispatchAsync_FailureStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.CancelConsignmentAfterDispatchAsync(_testConsignment);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelConsignmentAfterDispatchAsync_NetworkError()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var result = await _oAuthClientService.CancelConsignmentAfterDispatchAsync(_testConsignment);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelConsignmentAfterDispatchAsync_GeneralException()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var result = await _oAuthClientService.CancelConsignmentAfterDispatchAsync(_testConsignment);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelConsignmentAfterDispatchAsync_Timeout()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The operation was canceled due to a timeout."));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        await _oAuthClientService.CancelConsignmentAfterDispatchAsync(_testConsignment);

        // Assert
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //ReportReturnPackage
    [Fact]
    public async Task ReportReturnPackage_Success()
    {
        // Arrange
        var request = new ReportReturnPackageRequest { aldiReturnCode = "ARC123" };
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.ReportReturnPackage(request);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportReturnPackage_FailureStatusCode()
    {
        // Arrange
        var request = new ReportReturnPackageRequest { aldiReturnCode = "ARC123" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request", Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act
        var result = await _oAuthClientService.ReportReturnPackage(request);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportReturnPackage_NetworkError()
    {
        // Arrange
        var request = new ReportReturnPackageRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var result = await _oAuthClientService.ReportReturnPackage(request);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportReturnPackage_GeneralException()
    {
        // Arrange
        var request = new ReportReturnPackageRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error occurred"));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var result = await _oAuthClientService.ReportReturnPackage(request);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ReportReturnPackage_Timeout()
    {
        // Arrange
        var request = new ReportReturnPackageRequest { aldiReturnCode = "ARC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("The operation was canceled due to a timeout."));

        _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync("valid_token");

        // Act & Assert
        var result = await _oAuthClientService.ReportReturnPackage(request);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
    
    //UpdateApiOrderStatusInProgressAsync
    [Fact]
        public async Task UpdateApiOrderStatusInProgressAsync_ReturnsFalse_WhenOrderIsNull()
        {
            // Arrange

            // Act
            var result = await _oAuthClientService.UpdateApiOrderStatusInProgressAsync(null);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateApiOrderStatusInProgressAsync_ReturnsTrue_WhenStatusUpdatedSuccessfully()
        {
            // Arrange
            var order = new Order { Code = "testOrderCode" };
            var tokenResponse = "testAccessToken";
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put &&
                        req.RequestUri.ToString().Contains(order.Code)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _oAuthClientService.UpdateApiOrderStatusInProgressAsync(order);

            // Assert
            Assert.True(result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task UpdateApiOrderStatusInProgressAsync_RetriesOnUnauthorized()
        {
            // Arrange
            var order = new Order { Code = "testOrderCode" };
            var tokenResponse = "testAccessToken";
            var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK);

            _accessTokenServiceMock.SetupSequence(x => x.ValidateAndGetAccessToken())
                .ReturnsAsync(tokenResponse)
                .ReturnsAsync("newTestAccessToken");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put &&
                        req.RequestUri.ToString().Contains(order.Code)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse)
                .Callback(() => _mockHttpMessageHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Put &&
                            req.RequestUri.ToString().Contains(order.Code)),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(successResponse));

            _accessTokenServiceMock.Setup(x => x.GetAndUpdateNewAccessToken()).ReturnsAsync("newTestAccessToken");

            // Act
            var result = await _oAuthClientService.UpdateApiOrderStatusInProgressAsync(order);

            // Assert
            Assert.True(result);
            _accessTokenServiceMock.Verify(x => x.GetAndUpdateNewAccessToken(), Times.Once);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateApiOrderStatusInProgressAsync_ReturnsFalse_OnNonSuccessStatus()
        {
            // Arrange
            var order = new Order { Code = "testOrderCode" };
            var tokenResponse = "testAccessToken";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Put &&
                        req.RequestUri.ToString().Contains(order.Code)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _oAuthClientService.UpdateApiOrderStatusInProgressAsync(order);

            // Assert
            Assert.False(result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //CreateApiConsignmentAsync
        [Fact]
        public async Task CreateApiConsignmentAsync_Success()
        {
            // Arrange
            var consignmentRequestsList = new List<ConsignmentRequest> { new ConsignmentRequest { vendorConsignmentCode = "testCode" } };
            var orderCode = "testOrderCode";
            var tokenResponse = "testAccessToken";
            var consignmentListResponse = new ConsignmentListResponse();
            var responseContent = JsonConvert.SerializeObject(consignmentListResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains(orderCode)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestsList, orderCode);

            // Assert
            Assert.NotNull(result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task CreateApiConsignmentAsync_RetriesOnUnauthorized()
        {
            // Arrange
            var consignmentRequestsList = new List<ConsignmentRequest> { new ConsignmentRequest { vendorConsignmentCode = "testCode" } };
            var orderCode = "testOrderCode";
            var tokenResponse = "testAccessToken";
            var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new ConsignmentListResponse()), Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.SetupSequence(x => x.ValidateAndGetAccessToken())
                .ReturnsAsync(tokenResponse)
                .ReturnsAsync("newTestAccessToken");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains(orderCode)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse)
                .Callback(() => _mockHttpMessageHandler.Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Post &&
                            req.RequestUri.ToString().Contains(orderCode)),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(successResponse));

            _accessTokenServiceMock.Setup(x => x.GetAndUpdateNewAccessToken()).ReturnsAsync("newTestAccessToken");

            // Act
            var result = await _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestsList, orderCode);

            // Assert
            Assert.NotNull(result);
            _accessTokenServiceMock.Verify(x => x.GetAndUpdateNewAccessToken(), Times.Once);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateApiConsignmentAsync_ThrowsApiException_OnBadRequest()
        {
            // Arrange
            var consignmentRequestsList = new List<ConsignmentRequest> { new ConsignmentRequest { vendorConsignmentCode = "testCode" } };
            var orderCode = "testOrderCode";
            var tokenResponse = "testAccessToken";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains(orderCode)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() => _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestsList, orderCode));
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task CreateApiConsignmentAsync_ThrowsApiException_OnHttpRequestException()
        {
            // Arrange
            var consignmentRequestsList = new List<ConsignmentRequest> { new ConsignmentRequest { vendorConsignmentCode = "testCode" } };
            var orderCode = "testOrderCode";
            var tokenResponse = "testAccessToken";

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains(orderCode)),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error occurred"));

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() => _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestsList, orderCode));
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateApiConsignmentAsync_ThrowsApiException_OnUnexpectedException()
        {
            // Arrange
            var consignmentRequestsList = new List<ConsignmentRequest> { new ConsignmentRequest { vendorConsignmentCode = "testCode" } };
            var orderCode = "testOrderCode";
            var tokenResponse = "testAccessToken";

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri.ToString().Contains(orderCode)),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Unexpected error occurred"));

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(() => _oAuthClientService.CreateApiConsignmentAsync(consignmentRequestsList, orderCode));
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //GetApiReturnsWithStatusCreatedAsync
        [Fact]
        public async Task GetApiReturnsWithStatusCreatedAsync_Success()
        {
            // Arrange
            var status = "CREATED";
            var tokenResponse = "testAccessToken";
            var returnResponse = new ReturnResponse
            {
                ReturnRequests = new List<ReturnDto> { new ReturnDto { OrderCode = "123" } }
            };
            var responseContent = JsonConvert.SerializeObject(returnResponse);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(status);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ReturnRequests);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetApiReturnsWithStatusCreatedAsync_UnauthorizedRetry()
        {
            // Arrange
            var status = "CREATED";
            var tokenResponse = "testAccessToken";
            var newTokenResponse = "newTestAccessToken";
            var returnResponse = new ReturnResponse
            {
                ReturnRequests = new List<ReturnDto> { new ReturnDto() { OrderCode = "123" } }
            };
            var responseContent = JsonConvert.SerializeObject(returnResponse);
            var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _accessTokenServiceMock.Setup(x => x.GetAndUpdateNewAccessToken()).ReturnsAsync(newTokenResponse);
            _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse)
                .ReturnsAsync(successResponse);

            // Act
            var result = await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(status);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ReturnRequests);
            _accessTokenServiceMock.Verify(x => x.GetAndUpdateNewAccessToken(), Times.Once);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
             _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetApiReturnsWithStatusCreatedAsync_Failure()
        {
            // Arrange
            var status = "CREATED";
            var tokenResponse = "testAccessToken";
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);

            // Act
            var result = await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(status);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ReturnRequests);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetApiReturnsWithStatusCreatedAsync_UnauthorizedRetryFailure()
        {
            // Arrange
            var status = "CREATED";
            var tokenResponse = "testAccessToken";
            var newTokenResponse = "newTestAccessToken";
            var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            var failureResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Bad Request", Encoding.UTF8, "application/json")
            };

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _accessTokenServiceMock.Setup(x => x.GetAndUpdateNewAccessToken()).ReturnsAsync(newTokenResponse);
            _mockHttpMessageHandler.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(unauthorizedResponse)
                .ReturnsAsync(failureResponse);

            // Act
            var result = await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(status);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ReturnRequests);
            _accessTokenServiceMock.Verify(x => x.GetAndUpdateNewAccessToken(), Times.Once);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task GetApiReturnsWithStatusCreatedAsync_NetworkError()
        {
            // Arrange
            var status = "CREATED";
            var tokenResponse = "testAccessToken";

            _accessTokenServiceMock.Setup(x => x.ValidateAndGetAccessToken()).ReturnsAsync(tokenResponse);
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error occurred"));

            // Act & Assert
            await Assert.ThrowsAsync<ApiException>(async () => 
                await _oAuthClientService.GetApiReturnsWithStatusCreatedAsync(status));

            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        //ReturnInProgress

}