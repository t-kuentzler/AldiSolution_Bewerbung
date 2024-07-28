using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<ILogger<OrderService>> _loggerMock;
        private readonly Mock<IValidatorWrapper<SearchTerm>> _searchTermValidatorMock;
        private readonly Mock<IValidatorWrapper<CancelOrderEntryModel>> _cancelOrderEntryValidatorMock;
        private readonly Mock<ICancellationService> _cancellationServiceMock;
        private readonly Mock<IQuantityCheckService> _quantityCheckServiceMock;
        private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
        private readonly Mock<IValidatorWrapper<Order>> _orderValidatorMock;
        private readonly Mock<IValidatorWrapper<UpdateStatus>> _updateStatusValidatorMock;

        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _loggerMock = new Mock<ILogger<OrderService>>();
            _searchTermValidatorMock = new Mock<IValidatorWrapper<SearchTerm>>();
            _cancelOrderEntryValidatorMock = new Mock<IValidatorWrapper<CancelOrderEntryModel>>();
            _cancellationServiceMock = new Mock<ICancellationService>();
            _quantityCheckServiceMock = new Mock<IQuantityCheckService>();
            _oAuthClientServiceMock = new Mock<IOAuthClientService>();
            _orderValidatorMock = new Mock<IValidatorWrapper<Order>>();
            _updateStatusValidatorMock = new Mock<IValidatorWrapper<UpdateStatus>>();

            _orderService = new OrderService(
                _cancellationServiceMock.Object,
                _oAuthClientServiceMock.Object,
                _loggerMock.Object,
                _orderValidatorMock.Object,
                _updateStatusValidatorMock.Object,
                _orderRepositoryMock.Object,
                _searchTermValidatorMock.Object,
                _cancelOrderEntryValidatorMock.Object,
                _quantityCheckServiceMock.Object
            );
        }
        
        //ProcessSingleOrderAsync
        [Fact]
        public async Task ProcessSingleOrderAsync_ValidOrder_Success()
        {
            // Arrange
            var order = new Order { Code = "Order1" };

            _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
            _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).Returns(Task.CompletedTask);
            _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
            _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(order.Code, SharedStatus.InProgress)).ReturnsAsync(true);
            _oAuthClientServiceMock.Setup(s => s.UpdateApiOrderStatusInProgressAsync(order, 0)).ReturnsAsync(true);

            // Act
            await _orderService.ProcessSingleOrderAsync(order);

            // Assert
            _orderValidatorMock.Verify(v => v.ValidateAndThrowAsync(order), Times.Once);
            _orderRepositoryMock.Verify(r => r.CreateOrderAsync(order), Times.Once);
            _updateStatusValidatorMock.Verify(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>()), Times.Once);
            _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(order.Code, SharedStatus.InProgress), Times.Once);
            _oAuthClientServiceMock.Verify(s => s.UpdateApiOrderStatusInProgressAsync(order, 0), Times.Once);
        }

        [Fact]
        public async Task ProcessSingleOrderAsync_InvalidOrder_ThrowsValidationException()
        {
            // Arrange
            var order = new Order { Code = "Order1" };

            _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order))
                .ThrowsAsync(new ValidationException("Validation error"));
            
            // Act
            await _orderService.ProcessSingleOrderAsync(order);

            // Assert
            _orderValidatorMock.Verify(v => v.ValidateAndThrowAsync(order), Times.Once);
            _orderRepositoryMock.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Never);
            _updateStatusValidatorMock.Verify(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>()), Times.Never);
            _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _oAuthClientServiceMock.Verify(s => s.UpdateApiOrderStatusInProgressAsync(It.IsAny<Order>(), 0), Times.Never);
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
        public async Task ProcessSingleOrderAsync_UnexpectedError_LogsError()
        {
            // Arrange
            var order = new Order { Code = "Order1" };

            _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
            _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            await _orderService.ProcessSingleOrderAsync(order);

            // Assert
            _orderValidatorMock.Verify(v => v.ValidateAndThrowAsync(order), Times.Once);
            _orderRepositoryMock.Verify(r => r.CreateOrderAsync(order), Times.Once);
            _updateStatusValidatorMock.Verify(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>()), Times.Never);
            _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _oAuthClientServiceMock.Verify(s => s.UpdateApiOrderStatusInProgressAsync(It.IsAny<Order>(), 0), Times.Never);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

}