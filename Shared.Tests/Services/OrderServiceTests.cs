using FluentValidation;
using FluentValidation.Results;
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
    public async Task ProcessSingleOrderAsync_OrderValidationFails_LogsError()
    {
        // Arrange
        var order = new Order { Code = "Order1" };
        var validationResult = new ValidationResult(new[] { new ValidationFailure("Code", "Validation failed") });
        _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).ThrowsAsync(new ValidationException(validationResult.Errors));

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Fehler bei der Verarbeitung der Bestellung 'Order1'")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSingleOrderAsync_StatusValidationFails_LogsError()
    {
        // Arrange
        var order = new Order { Code = "Order1" };
        _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).Returns(Task.CompletedTask);
        var validationResult = new ValidationResult(new[] { new ValidationFailure("Status", "Validation failed") });
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).ThrowsAsync(new ValidationException(validationResult.Errors));

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Fehler bei der Verarbeitung der Bestellung 'Order1'")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSingleOrderAsync_OrderIsNull_AddOrderAsyncLogsError()
    {
        // Arrange
        Order order = null;

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Die Bestellung zum erstellen in der Datenbank ist null.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSingleOrderAsync_OrderIsNull_UpdateOrderStatusInDatabaseAsyncLogsError()
    {
        // Arrange
        var order = new Order { Code = "Order1" };
        _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).Returns(Task.CompletedTask);
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).ThrowsAsync(new OrderIsNullException("Die Bestellung zum Aktualisieren des Status ist null."));

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Die Bestellung zum Aktualisieren des Status ist null.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSingleOrderAsync_RepositoryException_LogsError()
    {
        // Arrange
        var order = new Order { Code = "Order1" };
        _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).Returns(Task.CompletedTask);
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(order.Code, SharedStatus.InProgress)).ThrowsAsync(new RepositoryException("Repository error"));

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Repository-Exception bei der Verarbeitung der Bestellung 'Order1'")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessSingleOrderAsync_UnexpectedException_LogsError()
    {
        // Arrange
        var order = new Order { Code = "Order1" };
        _orderValidatorMock.Setup(v => v.ValidateAndThrowAsync(order)).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.CreateOrderAsync(order)).Returns(Task.CompletedTask);
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(order.Code, SharedStatus.InProgress)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        await _orderService.ProcessSingleOrderAsync(order);

        // Assert
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Ein unerwarteter Fehler ist bei der Verarbeitung der Bestellung 'Order1' aufgetreten")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
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
    
    //GetOrderStatusByOrderCodeAsync
    [Fact]
    public async Task GetOrderStatusByOrderCodeAsync_ValidOrderCode_ReturnsOrderStatus()
    {
        // Arrange
        var orderCode = "Order1";
        var expectedStatus = "InProgress";
        _orderRepositoryMock.Setup(r => r.GetOrderStatusByOrderCodeAsync(orderCode)).ReturnsAsync(expectedStatus);

        // Act
        var result = await _orderService.GetOrderStatusByOrderCodeAsync(orderCode);

        // Assert
        Assert.Equal(expectedStatus, result);
        _orderRepositoryMock.Verify(r => r.GetOrderStatusByOrderCodeAsync(orderCode), Times.Once);
    }

    [Fact]
    public async Task GetOrderStatusByOrderCodeAsync_OrderStatusIsNull_ThrowsOrderStatusIsNullException()
    {
        // Arrange
        var orderCode = "Order1";
        _orderRepositoryMock.Setup(r => r.GetOrderStatusByOrderCodeAsync(orderCode)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<OrderStatusIsNullException>(() => _orderService.GetOrderStatusByOrderCodeAsync(orderCode));
        Assert.Equal("OrderStatusIsNullException", exception.GetType().Name);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Der Status für die Order mit dem OrderCode '{orderCode}' ist null oder empty.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderStatusByOrderCodeAsync_RepositoryException_ThrowsAndLogsRepositoryException()
    {
        // Arrange
        var orderCode = "Order1";
        var repositoryException = new RepositoryException("Repository error");
        _orderRepositoryMock.Setup(r => r.GetOrderStatusByOrderCodeAsync(orderCode)).ThrowsAsync(repositoryException);

        // Act
        var exception = await Assert.ThrowsAsync<RepositoryException>(() => _orderService.GetOrderStatusByOrderCodeAsync(orderCode));

        // Assert
        Assert.Equal(repositoryException, exception);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Repository-Exception beim Abrufen von Order mit dem OrderCode '{orderCode}'")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderStatusByOrderCodeAsync_UnexpectedException_ThrowsAndLogsOrderServiceException()
    {
        // Arrange
        var orderCode = "Order1";
        var unexpectedException = new Exception("Unexpected error");
        _orderRepositoryMock.Setup(r => r.GetOrderStatusByOrderCodeAsync(orderCode)).ThrowsAsync(unexpectedException);

        // Act
        var exception = await Assert.ThrowsAsync<OrderServiceException>(() => _orderService.GetOrderStatusByOrderCodeAsync(orderCode));

        // Assert
        Assert.Equal("Fehler beim Abrufen der Order mit dem OrderCode 'Order1'.", exception.Message);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Unerwarteter Fehler beim Abrufen von Order mit dem OrderCode '{orderCode}'")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    //UpdateSingleOrderStatusInDatabaseAsync
    [Fact]
    public async Task UpdateSingleOrderStatusInDatabaseAsync_ValidOrderCodeAndStatus_ReturnsTrue()
    {
        // Arrange
        var orderCode = "Order1";
        var status = "Shipped";
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(orderCode, status)).ReturnsAsync(true);

        // Act
        var result = await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);

        // Assert
        Assert.True(result);
        _updateStatusValidatorMock.Verify(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>()), Times.Once);
        _orderRepositoryMock.Verify(r => r.UpdateOrderStatusAsync(orderCode, status), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Bestellung mit Code {orderCode} wurde erfolgreich in der Datenbank auf {status} aktualisiert.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSingleOrderStatusInDatabaseAsync_OrderCodeIsNullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var orderCode = string.Empty;
        var status = "Shipped";

        // Act
        var result = await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("OrderCode darf beim aktualisieren des Order Status nicht leer sein.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSingleOrderStatusInDatabaseAsync_StatusValidationFails_ReturnsFalse()
    {
        // Arrange
        var orderCode = "Order1";
        var status = "Shipped";
        var validationResult = new ValidationResult(new[] { new ValidationFailure("Status", "Validation failed") });
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).ThrowsAsync(new ValidationException(validationResult.Errors));

        // Act
        var result = await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Bei der Aktualisierung des Status {status} mit dem Code {orderCode} ist ein Fehler aufgetreten: Validation failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSingleOrderStatusInDatabaseAsync_UpdateOrderStatusAsyncFails_ReturnsFalse()
    {
        // Arrange
        var orderCode = "Order1";
        var status = "Shipped";
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(orderCode, status)).ReturnsAsync(false);

        // Act
        var result = await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Fehler beim Aktualisieren des Status in der Datenbank für Bestellung mit Code {orderCode}.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSingleOrderStatusInDatabaseAsync_UnexpectedException_ReturnsFalse()
    {
        // Arrange
        var orderCode = "Order1";
        var status = "Shipped";
        _updateStatusValidatorMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<UpdateStatus>())).Returns(Task.CompletedTask);
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusAsync(orderCode, status)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _orderService.UpdateSingleOrderStatusInDatabaseAsync(orderCode, status);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Unerwarteter Fehler beim aktualisieren des Status der Order mit dem OrderCode '{orderCode}'.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    //GetOrdersByStatusAsync
     [Fact]
    public async Task GetOrdersByStatusAsync_ValidStatus_ReturnsOrders()
    {
        // Arrange
        var status = "Pending";
        var expectedOrders = new List<Order>
        {
            new Order { Code = "Order1" },
            new Order { Code = "Order2" }
        };
        _orderRepositoryMock.Setup(r => r.GetOrdersWithStatusAsync(status)).ReturnsAsync(expectedOrders);

        // Act
        var result = await _orderService.GetOrdersByStatusAsync(status);

        // Assert
        Assert.Equal(expectedOrders, result);
        _orderRepositoryMock.Verify(r => r.GetOrdersWithStatusAsync(status), Times.Once);
        _loggerMock.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Fact]
    public async Task GetOrdersByStatusAsync_RepositoryException_LogsErrorAndReturnsEmptyList()
    {
        // Arrange
        var status = "Pending";
        _orderRepositoryMock.Setup(r => r.GetOrdersWithStatusAsync(status)).ThrowsAsync(new RepositoryException("Repository error"));

        // Act
        var result = await _orderService.GetOrdersByStatusAsync(status);

        // Assert
        Assert.Empty(result);
        _orderRepositoryMock.Verify(r => r.GetOrdersWithStatusAsync(status), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Repository-Exception beim Abrufen von allen Bestellungen mit dem Status '{status}'.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task GetOrdersByStatusAsync_UnexpectedException_LogsErrorAndReturnsEmptyList()
    {
        // Arrange
        var status = "Pending";
        _orderRepositoryMock.Setup(r => r.GetOrdersWithStatusAsync(status)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _orderService.GetOrdersByStatusAsync(status);

        // Assert
        Assert.Empty(result);
        _orderRepositoryMock.Verify(r => r.GetOrdersWithStatusAsync(status), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Unerwarteter Fehler beim Abrufen von allen Bestellungen mit dem Status '{status}'.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    //UpdateOrderStatusByOrderCodeAsync
    [Fact]
    public async Task UpdateOrderStatusByOrderCodeAsync_ValidOrderCode_UpdatesStatusSuccessfully()
    {
        // Arrange
        var orderCode = "Order1";
        var newStatus = "Completed";
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus))
                            .Returns(Task.CompletedTask);

        // Act
        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus);

        // Assert
        _orderRepositoryMock.Verify(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Der Status der Bestellung mit dem OrderCode '{orderCode}' wurde in der Datenbank erfolgreich auf '{newStatus}' aktualisiert.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusByOrderCodeAsync_RepositoryException_LogsError()
    {
        // Arrange
        var orderCode = "Order1";
        var newStatus = "Completed";
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus))
                            .ThrowsAsync(new RepositoryException("Repository error"));

        // Act
        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus);

        // Assert
        _orderRepositoryMock.Verify(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Repository-Exception beim aktualisieren des Status '{newStatus}' für die Bestellung mit dem OrderCode '{orderCode}'.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusByOrderCodeAsync_UnexpectedException_LogsError()
    {
        // Arrange
        var orderCode = "Order1";
        var newStatus = "Completed";
        _orderRepositoryMock.Setup(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus))
                            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        await _orderService.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus);

        // Assert
        _orderRepositoryMock.Verify(r => r.UpdateOrderStatusByOrderCodeAsync(orderCode, newStatus), Times.Once);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Unerwarteter Fehler beim aktualisieren des Status '{newStatus}' für die Bestellung mit dem OrderCode '{orderCode}'.")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
    
    //UpdateOrderStatusByIdAsync
    [Fact]
    public async Task UpdateOrderStatusByIdAsync_UpdatesStatusSuccessfully()
    {
        // Arrange
        int orderId = 1;
        string status = "Shipped";

        _orderRepositoryMock.Setup(x => x.UpdateOrderStatusByIdAsync(orderId, status))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.UpdateOrderStatusByIdAsync(orderId, status);

        // Assert
        _orderRepositoryMock.Verify(x => x.UpdateOrderStatusByIdAsync(orderId, status), Times.Once);
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
    public async Task UpdateOrderStatusByIdAsync_ThrowsRepositoryException_OnRepositoryFailure()
    {
        // Arrange
        int orderId = 1;
        string status = "Cancelled";
        var repositoryException = new RepositoryException("Database error");

        _orderRepositoryMock.Setup(x => x.UpdateOrderStatusByIdAsync(orderId, status))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _orderService.UpdateOrderStatusByIdAsync(orderId, status));

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
    public async Task UpdateOrderStatusByIdAsync_ThrowsOrderServiceException_OnUnexpectedError()
    {
        // Arrange
        int orderId = 1;
        string status = "Processing";
        var unexpectedException = new Exception("Unexpected error");

        _orderRepositoryMock.Setup(x => x.UpdateOrderStatusByIdAsync(orderId, status))
            .ThrowsAsync(unexpectedException);

        // Act & Assert
        await Assert.ThrowsAsync<OrderServiceException>(() => _orderService.UpdateOrderStatusByIdAsync(orderId, status));

        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);    
    }
    
    //GetALlOrdersByStatusAsync
    [Fact]
    public async Task GetAllOrdersByStatusAsync_ReturnsOrders_WhenStatusIsValid()
    {
        // Arrange
        var status = "DELIVERED";
        var orders = new List<Order>
        {
            new Order()
            {
                Id = 1,
                Consignments = new List<Consignment>()
                {
                }
            },
            new Order()
            {
                Id = 2,
                Consignments = new List<Consignment>()
                {
                }
            },
        };
    
        _orderRepositoryMock.Setup(repo => repo.GetOrdersWithStatusAsync(status))
            .ReturnsAsync(orders);
    
        // Act
        var result = await _orderService.GetAllOrdersByStatusAsync(status);
    
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Erwarten Sie die gleiche Anzahl von Orders, die Sie im Setup festgelegt haben
        _orderRepositoryMock.Verify(repo => repo.GetOrdersWithStatusAsync(status), Times.Once);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAllOrdersByStatusAsync_ThrowsArgumentException_WhenStatusIsNullOrEmpty(string status)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetAllOrdersByStatusAsync(status));
    
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
    public async Task GetAllOrdersByStatusAsync_ThrowsRepositoryException_WhenRepositoryExceptionOccurs()
    {
        // Arrange
        var status = "SHIPPED";
    
        _orderRepositoryMock.Setup(repo => repo.GetOrdersWithStatusAsync(status))
            .ThrowsAsync(new RepositoryException("Test exception"));
    
        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _orderService.GetAllOrdersByStatusAsync(status));
    
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
    public async Task GetAllOrdersByStatusAsync_ThrowsOrderServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var status = "PROCESSING";
    
        _orderRepositoryMock.Setup(repo => repo.GetOrdersWithStatusAsync(status))
            .ThrowsAsync(new Exception("Unexpected error"));
    
        // Act & Assert
        await Assert.ThrowsAsync<OrderServiceException>(() => _orderService.GetAllOrdersByStatusAsync(status));
    
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
    public async Task GetAllOrdersByStatusAsync_ReturnsEmptyList_WhenNoOrdersFound()
    {
        // Arrange
        var status = "CANCELLED";
        _orderRepositoryMock.Setup(repo => repo.GetOrdersWithStatusAsync(status)).ReturnsAsync(new List<Order>());
    
        // Act
        var result = await _orderService.GetAllOrdersByStatusAsync(status);
    
        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    //GetOrderByIdAsync
    [Fact]
    public async Task GetOrderByIdAsync_ReturnsOrder_WhenOrderExist()
    {
        //Arrange
        int orderId = 1;
    
        var order = new Order()
        {
            Id = 1
        };
    
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId))
            .ReturnsAsync(order);
        // Act
        var result = await _orderService.GetOrderByIdAsync(orderId);
    
        // Assert
        Assert.NotNull(result);
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetOrderByIdAsync_ThrowsArgumentException_WhenOrderIdIsNegative(int orderId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _orderService.GetOrderByIdAsync(orderId));
    
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
    public async Task GetOrderByIdAsync_ThrowsRepositoryException_WhenRepositoryThrowsRepositoryException()
    {
        // Arrange
        int orderId = 1;
    
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId))
            .ThrowsAsync(new RepositoryException("Test exception"));
    
        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _orderService.GetOrderByIdAsync(orderId));
    
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
    
    [Fact]
    public async Task GetOrderByIdAsync_ThrowsOrderServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        int orderId = 1;
    
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(orderId))
            .ThrowsAsync(new Exception("Unexpected error"));
    
        // Act & Assert
        await Assert.ThrowsAsync<OrderServiceException>(() => _orderService.GetOrderByIdAsync(orderId));
    
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
    
    [Fact]
    public async Task GetOrderByIdAsync_ThrowsOrderIsNullException_WhenOrderDoesNotExist()
    {
        // Arrange
        int nonExistingOrderId = 99;
        _orderRepositoryMock.Setup(repo => repo.GetOrderByIdAsync(nonExistingOrderId))
            .ReturnsAsync((Order?)null);

        // Act & Assert
        await Assert.ThrowsAsync<OrderIsNullException>(() => _orderService.GetOrderByIdAsync(nonExistingOrderId));

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