using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;
using Shared.Services;
using Xunit;

namespace Shared.Tests.Services
{
    public class OrderProcessingServiceTests
    {
        private readonly Mock<IAccessTokenService> _accessTokenServiceMock;
        private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ILogger<OrderProcessingService>> _loggerMock;
        private readonly OrderProcessingService _orderProcessingService;

        public OrderProcessingServiceTests()
        {
            _accessTokenServiceMock = new Mock<IAccessTokenService>();
            _oAuthClientServiceMock = new Mock<IOAuthClientService>();
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<OrderProcessingService>>();
            _orderProcessingService = new OrderProcessingService(
                _accessTokenServiceMock.Object,
                _oAuthClientServiceMock.Object,
                _orderServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ProcessOpenOrdersAsync_NoOrders_LogsInformation()
        {
            // Arrange
            _accessTokenServiceMock.Setup(x => x.EnsureTokenDataExists()).Returns(Task.CompletedTask);
            _oAuthClientServiceMock.Setup(x => x.GetApiOrdersAsync())
                .ReturnsAsync(new OrderResponse { Orders = new List<Order>() });

            // Act
            await _orderProcessingService.ProcessOpenOrdersAsync();

            // Assert
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
        public async Task ProcessOpenOrdersAsync_OrdersExist_ProcessesEachOrder()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { Id = 1, Code = "Order1" },
                new Order { Id = 2, Code = "Order2" }
            };

            _accessTokenServiceMock.Setup(x => x.EnsureTokenDataExists()).Returns(Task.CompletedTask);
            _oAuthClientServiceMock.Setup(x => x.GetApiOrdersAsync())
                .ReturnsAsync(new OrderResponse { Orders = orders });

            // Act
            await _orderProcessingService.ProcessOpenOrdersAsync();

            // Assert
            _orderServiceMock.Verify(x => x.ProcessSingleOrderAsync(It.IsAny<Order>()), Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessOpenOrdersAsync_ExceptionThrown_LogsError()
        {
            // Arrange
            _accessTokenServiceMock.Setup(x => x.EnsureTokenDataExists()).ThrowsAsync(new Exception("Test exception"));

            // Act
            await _orderProcessingService.ProcessOpenOrdersAsync();

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
    }
}
