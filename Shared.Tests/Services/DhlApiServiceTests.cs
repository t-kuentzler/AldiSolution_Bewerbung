using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Shared.Contracts;
using Shared.Models;
using Shared.Services;
using Xunit;

namespace Shared.Tests.Services
{
    public class DhlApiServiceTests
    {
        private readonly Mock<ILogger<DhlApiService>> _loggerMock;
        private readonly Mock<IOptions<DhlSettings>> _dhlSettingsMock;
        private readonly DhlApiService _dhlApiService;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

        public DhlApiServiceTests()
        {
            _loggerMock = new Mock<ILogger<DhlApiService>>();
            _dhlSettingsMock = new Mock<IOptions<DhlSettings>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            var dhlSettings = new DhlSettings
            {
                BaseUrl = "https://api.dhl.com/",
                ApiKey = "test-api-key"
            };
            _dhlSettingsMock.Setup(s => s.Value).Returns(dhlSettings);

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _dhlApiService = new DhlApiService(_loggerMock.Object, _dhlSettingsMock.Object, httpClient);
        }

        [Fact]
        public async Task GetTrackingStatusFromApiAsync_ReturnsStatusCode_WhenApiCallIsSuccessful()
        {
            // Arrange
            var trackingNumber = "1234567890";
            var expectedStatusCode = "delivered";

            var shipmentResponse = new ShipmentResponse
            {
                Shipments = new[]
                {
                    new Shipment
                    {
                        Status = new DhlStatus()
                        {
                            StatusCode = expectedStatusCode
                        }
                    }
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(shipmentResponse))
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _dhlApiService.GetTrackingStatusFromApiAsync(trackingNumber);

            // Assert
            Assert.Equal(expectedStatusCode, result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetTrackingStatusFromApiAsync_ReturnsEmptyString_WhenApiCallFails()
        {
            // Arrange
            var trackingNumber = "1234567890";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _dhlApiService.GetTrackingStatusFromApiAsync(trackingNumber);

            // Assert
            Assert.Equal(string.Empty, result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTrackingStatusFromApiAsync_ReturnsEmptyString_WhenNoShipmentsFound()
        {
            // Arrange
            var trackingNumber = "1234567890";

            var shipmentResponse = new ShipmentResponse
            {
                Shipments = Array.Empty<Shipment>()
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(shipmentResponse))
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _dhlApiService.GetTrackingStatusFromApiAsync(trackingNumber);

            // Assert
            Assert.Equal(string.Empty, result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetTrackingStatusFromApiAsync_ReturnsEmptyString_WhenStatusIsNull()
        {
            // Arrange
            var trackingNumber = "1234567890";

            var shipmentResponse = new ShipmentResponse
            {
                Shipments = new[]
                {
                    new Shipment
                    {
                        Status = null
                    }
                }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(shipmentResponse))
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _dhlApiService.GetTrackingStatusFromApiAsync(trackingNumber);

            // Assert
            Assert.Equal(string.Empty, result);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
