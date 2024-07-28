using FluentValidation;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
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
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>()))
            .Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(order.Code, SharedStatus.InProgress))
            .ReturnsAsync(true);
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


    //GetOrderByOrderCodeAsync
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOrderByOrderCodeAsync_ThrowsArgumentException_WhenOrderCodeIsNullOrEmpty(string orderCode)
    {
        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetOrderByOrderCodeAsync(orderCode));
        Assert.Equal(nameof(orderCode), exception.ParamName);
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
    public async Task GetOrderByOrderCodeAsync_ReturnsOrder_WhenOrderExists()
    {
        // Arrange
        var orderCode = "Order123";
        var expectedOrder = new Order { Id = 1, Code = orderCode };

        _orderRepositoryMock.Setup(repo => repo.GetOrderByOrderCodeAsync(orderCode))
            .ReturnsAsync(expectedOrder);

        // Act
        var result = await _orderService.GetOrderByOrderCodeAsync(orderCode);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedOrder, result);
    }

    [Fact]
    public async Task GetOrderByOrderCodeAsync_ThrowsOrderIsNullException_WhenOrderNotFound()
    {
        // Arrange
        var orderCode = "UnknownCode";
        _orderRepositoryMock.Setup(repo => repo.GetOrderByOrderCodeAsync(orderCode))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<OrderIsNullException>(() => _orderService.GetOrderByOrderCodeAsync(orderCode));
    }

    [Fact]
    public async Task GetOrderByOrderCodeAsync_ThrowsRepositoryException_OnRepositoryFailure()
    {
        // Arrange
        var orderCode = "Order123";
        var repositoryException = new RepositoryException("Database error");

        _orderRepositoryMock.Setup(repo => repo.GetOrderByOrderCodeAsync(orderCode))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _orderService.GetOrderByOrderCodeAsync(orderCode));
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
    public async Task GetOrderByOrderCodeAsync_ThrowsOrderServiceException_OnUnexpectedError()
    {
        // Arrange
        var orderCode = "Order123";
        var unexpectedException = new Exception("Unexpected error");

        _orderRepositoryMock.Setup(repo => repo.GetOrderByOrderCodeAsync(orderCode))
            .ThrowsAsync(unexpectedException);

        // Act & Assert
        var exception =
            await Assert.ThrowsAsync<OrderServiceException>(() => _orderService.GetOrderByOrderCodeAsync(orderCode));
        Assert.IsType<OrderServiceException>(exception);
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