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

public class CancellationServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<ILogger<CancellationService>> _loggerMock;
    private readonly CancellationService _cancellationService;
    private readonly Mock<IValidatorWrapper<ProcessCancellationEntry>> _processCancellationEntryMock;


    public CancellationServiceTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _loggerMock = new Mock<ILogger<CancellationService>>();
        _processCancellationEntryMock = new Mock<IValidatorWrapper<ProcessCancellationEntry>>();
        _cancellationService = new CancellationService(_loggerMock.Object, _orderRepositoryMock.Object,
            _processCancellationEntryMock.Object);
    }

    //ProcessCancellationEntry
    [Fact]
    public async Task ProcessCancellationEntry_CancelsFullOrderEntry()
    {
        // Arrange
        var order = new Order();
        var orderEntry = new OrderEntry { Quantity = 10, CanceledOrReturnedQuantity = 0 };
        var cancellationEntry = new OrderCancellationEntry { cancelQuantity = 10 };

        // Act
        await _cancellationService.ProcessCancellationEntry(order, orderEntry, cancellationEntry);

        // Assert
        Assert.Equal(10, orderEntry.CanceledOrReturnedQuantity);

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
    public async Task ProcessCancellationEntry_CancelsPartialOrderEntry()
    {
        // Arrange
        var order = new Order();
        var orderEntry = new OrderEntry { Quantity = 10, CanceledOrReturnedQuantity = 5 };
        var cancellationEntry = new OrderCancellationEntry { cancelQuantity = 3 };

        // Act
        await _cancellationService.ProcessCancellationEntry(order, orderEntry, cancellationEntry);

        // Assert
        Assert.Equal(8, orderEntry.CanceledOrReturnedQuantity);

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
    public async Task ProcessCancellationEntry_ValidationException()
    {
        // Arrange
        var order = new Order();
        var orderEntry = new OrderEntry();
        var cancellationEntry = new OrderCancellationEntry();
        _processCancellationEntryMock.Setup(v => v.ValidateAndThrowAsync(It.IsAny<ProcessCancellationEntry>()))
            .ThrowsAsync(new ValidationException("Validation failed"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _cancellationService.ProcessCancellationEntry(order, orderEntry, cancellationEntry));
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
    public async Task ProcessCancellationEntry_RepositoryException()
    {
        // Arrange
        var order = new Order();
        var orderEntry = new OrderEntry();
        var cancellationEntry = new OrderCancellationEntry();
        _orderRepositoryMock.Setup(r => r.UpdateOrderEntryAsync(It.IsAny<OrderEntry>()))
            .ThrowsAsync(new RepositoryException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _cancellationService.ProcessCancellationEntry(order, orderEntry, cancellationEntry));
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
    public async Task ProcessCancellationEntry_UnexpectedException()
    {
        // Arrange
        var order = new Order();
        var orderEntry = new OrderEntry();
        var cancellationEntry = new OrderCancellationEntry();
        _orderRepositoryMock.Setup(r => r.UpdateOrderEntryAsync(It.IsAny<OrderEntry>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<CancellationServiceException>(() =>
            _cancellationService.ProcessCancellationEntry(order, orderEntry, cancellationEntry));
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //AreAllOrderEntriesCancelled
    [Fact]
    public void AreAllOrderEntriesCancelled_NullOrder_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<OrderIsNullException>(() => _cancellationService.AreAllOrderEntriesCancelled(null));

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
    public void AreAllOrderEntriesCancelled_NullOrderEntries_ThrowsException()
    {
        // Arrange
        var order = new Order { Entries = null };

        // Act & Assert
        Assert.Throws<OrderEntryIsNullException>(() => _cancellationService.AreAllOrderEntriesCancelled(order));

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
    public void AreAllOrderEntriesCancelled_AllEntriesCancelled_ReturnsTrue()
    {
        // Arrange
        var order = new Order
        {
            Entries = new List<OrderEntry>
            {
                new OrderEntry { Quantity = 10, CanceledOrReturnedQuantity = 10 },
                new OrderEntry { Quantity = 5, CanceledOrReturnedQuantity = 5 }
            }
        };

        // Act
        var result = _cancellationService.AreAllOrderEntriesCancelled(order);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllOrderEntriesCancelled_NotAllEntriesCancelled_ReturnsFalse()
    {
        // Arrange
        var order = new Order
        {
            Entries = new List<OrderEntry>
            {
                new OrderEntry { Quantity = 10, CanceledOrReturnedQuantity = 5 },
                new OrderEntry { Quantity = 5, CanceledOrReturnedQuantity = 5 }
            }
        };

        // Act
        var result = _cancellationService.AreAllOrderEntriesCancelled(order);

        // Assert
        Assert.False(result);
    }

    //CancelWholeOrder
    [Fact]
    public async Task CancelWholeOrder_ThrowsOrderIsNullException_WhenOrderIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<OrderIsNullException>(() => _cancellationService.CancelWholeOrder(null));

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
    public async Task CancelWholeOrder_SuccessfullyCanceled()
    {
        // Arrange
        var order = new Order { Code = "123", Status = "TEST" };

        // Act
        await _cancellationService.CancelWholeOrder(order);

        // Assert
        Assert.Equal(SharedStatus.Canceled, order.Status);
        _orderRepositoryMock.Verify(r => r.UpdateOrderAsync(order), Times.Once);

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
    public async Task CancelWholeOrder_RepositoryException_ThrowsException()
    {
        // Arrange
        var order = new Order { Code = "123", Status = "TEST" };
        _orderRepositoryMock.Setup(r => r.UpdateOrderAsync(order))
            .ThrowsAsync(new RepositoryException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() => _cancellationService.CancelWholeOrder(order));

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
    public async Task CancelWholeOrder_UnexpectedException_ThrowsCancellationServiceException()
    {
        // Arrange
        var order = new Order { Code = "123", Status = "TEST" };
        _orderRepositoryMock.Setup(r => r.UpdateOrderAsync(order)).ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<CancellationServiceException>(() => _cancellationService.CancelWholeOrder(order));

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