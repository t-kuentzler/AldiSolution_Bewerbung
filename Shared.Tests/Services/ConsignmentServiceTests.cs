using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

public class ConsignmentServiceTests
{
    private readonly Mock<IConsignmentRepository> _consignmentRepositoryMock;
    private readonly Mock<ILogger<ConsignmentService>> _loggerMock;
    private readonly Mock<IValidatorWrapper<SearchTerm>> _searchTermValidatorMock;
    private readonly ConsignmentService _consignmentService;

    public ConsignmentServiceTests()
    {
        _consignmentRepositoryMock = new Mock<IConsignmentRepository>();
        _loggerMock = new Mock<ILogger<ConsignmentService>>();
        _searchTermValidatorMock = new Mock<IValidatorWrapper<SearchTerm>>();
        _consignmentService = new ConsignmentService(_consignmentRepositoryMock.Object, _loggerMock.Object,
            _searchTermValidatorMock.Object);
    }

    //SaveConsignmentsAsync
    [Fact]
    public async Task SaveConsignmentAsync_Success()
    {
        // Arrange
        var consignment = new Consignment { VendorConsignmentCode = "VC123" };
        var expectedConsignmentId = 1;

        _consignmentRepositoryMock
            .Setup(repo => repo.SaveConsignmentAsync(consignment))
            .ReturnsAsync((true, expectedConsignmentId));

        // Act
        var result = await _consignmentService.SaveConsignmentAsync(consignment);

        // Assert
        Assert.True(result.success);
        Assert.Equal(expectedConsignmentId, result.consignmentId);
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
    public async Task SaveConsignmentAsync_Failure()
    {
        // Arrange
        var consignment = new Consignment { VendorConsignmentCode = "VC123" };

        _consignmentRepositoryMock
            .Setup(repo => repo.SaveConsignmentAsync(consignment))
            .ReturnsAsync((false, 0));

        // Act
        var result = await _consignmentService.SaveConsignmentAsync(consignment);

        // Assert
        Assert.False(result.success);
        Assert.Equal(0, result.consignmentId);

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
    public async Task SaveConsignmentAsync_ThrowsConsignmentServiceException_WhenExceptionOccurs()
    {
        // Arrange
        var consignment = new Consignment { VendorConsignmentCode = "VC123" };

        _consignmentRepositoryMock
            .Setup(repo => repo.SaveConsignmentAsync(consignment))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var exception = await Assert.ThrowsAsync<ConsignmentServiceException>(
            () => _consignmentService.SaveConsignmentAsync(consignment));

        // Assert
        Assert.Equal("Fehler beim Speichern des Consignment mit dem VendorConsignmentCode 'VC123'.", exception.Message);
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
    public async Task SaveConsignmentAsync_LogsErrorForInvalidConsignment()
    {
        // Arrange
        var consignment = new Consignment { VendorConsignmentCode = null }; // Ungültige Daten

        _consignmentRepositoryMock
            .Setup(repo => repo.SaveConsignmentAsync(consignment))
            .ReturnsAsync((false, 0));

        // Act
        await _consignmentService.SaveConsignmentAsync(consignment);

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


    //GetConsignmentByIdAsync
    [Fact]
    public async Task GetConsignmentByIdAsync_ReturnsConsignment_WhenConsignmentExist()
    {
        //Arrange
        int consignmentId = 1;

        var consignment = new Consignment()
        {
            Id = 1
        };

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentByIdAsync(consignmentId))
            .ReturnsAsync(consignment);
        // Act
        var result = await _consignmentService.GetConsignmentByIdAsync(consignmentId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetConsignmentByIdAsync_ReturnsNull_WhenConsignmentDoesNotExist()
    {
        // Arrange
        int nonExistingConsignmentId = 99;
        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentByIdAsync(nonExistingConsignmentId))
            .ReturnsAsync((Consignment?)null);

        // Act
        var result = await _consignmentService.GetConsignmentByIdAsync(nonExistingConsignmentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetConsignmentByIdAsync_ThrowsRepositoryException_WhenRepositoryThrowsRepositoryException()
    {
        // Arrange
        int consignmentId = 1;

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentByIdAsync(consignmentId))
            .ThrowsAsync(new RepositoryException("Test exception"));

        // Act
        await Assert.ThrowsAsync<RepositoryException>(() => _consignmentService.GetConsignmentByIdAsync(consignmentId));

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
    public async Task GetConsignmentByIdAsync_ThrowsConsignmentServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        int consignmentId = 1;

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentByIdAsync(consignmentId))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.GetConsignmentByIdAsync(consignmentId));

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

    //UpdateConsignmentAsync
    [Fact]
    public async Task UpdateConsignmentAsync_Success()
    {
        // Arrange
        var consignment = new Consignment { Id = 1 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .Returns(Task.CompletedTask);

        // Act
        await _consignmentService.UpdateConsignmentAsync(consignment);

        // Assert
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
    public async Task UpdateConsignmentAsync_ThrowsRepositoryException_WhenRepositoryExceptionOccurs()
    {
        // Arrange
        var consignment = new Consignment { Id = 1 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .ThrowsAsync(new RepositoryException("Repository error"));

        // Act
        var exception = await Assert.ThrowsAsync<RepositoryException>(
            () => _consignmentService.UpdateConsignmentAsync(consignment));

        // Assert
        Assert.Equal("Repository error", exception.Message);
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
    public async Task UpdateConsignmentAsync_ThrowsConsignmentServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var consignment = new Consignment { Id = 1 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var exception = await Assert.ThrowsAsync<ConsignmentServiceException>(
            () => _consignmentService.UpdateConsignmentAsync(consignment));

        // Assert
        Assert.Equal("Unerwarteter Fehler beim aktualisieren des Consignment mit der Id '1' in der Datenbank.",
            exception.Message);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //ParseConsignmentToConsignmentRequest
    [Fact]
    public void ParseConsignmentToConsignmentRequest_Success()
    {
        // Arrange
        var consignment = new Consignment
        {
            Order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry
                    {
                        DeliveryAddress = new DeliveryAddress
                        {
                            Type = "Type1",
                            CountryIsoCode = "DE"
                        }
                    }
                }
            },
            ConsignmentEntries = new List<ConsignmentEntry>
            {
                new ConsignmentEntry
                {
                    OrderEntryNumber = 2,
                    Quantity = 10
                }
            },
            Carrier = "Carrier1",
            ShippingDate = DateTime.Now,
            Status = "Status1",
            StatusText = "Status Text",
            TrackingId = "Tracking1",
            VendorConsignmentCode = "Vendor1"
        };

        // Act
        var result = _consignmentService.ParseConsignmentToConsignmentRequest(consignment);

        // Assert
        Assert.Single(result);
        var consignmentRequest = result.First();
        Assert.Equal("Carrier1", consignmentRequest.carrier);
        Assert.Equal("Type1", consignmentRequest.shippingAddress.type);
        Assert.Equal("DE", consignmentRequest.shippingAddress.countryIsoCode);
        Assert.Equal(2, consignmentRequest.entries.First().orderEntryNumber);
        Assert.Equal(10, consignmentRequest.entries.First().quantity);
        Assert.Equal(consignment.ShippingDate.ToString("yyyy-MM-dd"), consignmentRequest.shippingDate);
        Assert.Equal("Status1", consignmentRequest.status);
        Assert.Equal("Status Text", consignmentRequest.statusText);
        Assert.Equal("Tracking1", consignmentRequest.trackingId);
        Assert.Equal("Vendor1", consignmentRequest.vendorConsignmentCode);
    }

    [Fact]
    public void ParseConsignmentToConsignmentRequest_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Arrange
        var consignment = new Consignment
        {
            Order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry
                    {
                        DeliveryAddress = new DeliveryAddress
                        {
                            Type = null,
                            CountryIsoCode = "DE"
                        }
                    }
                }
            }
        };

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _consignmentService.ParseConsignmentToConsignmentRequest(consignment));

        // Assert
        Assert.Equal("firstEntryType", exception.ParamName);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) =>
                    state.ToString().Contains("Type ist null beim konvertieren der Consignment")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public void ParseConsignmentToConsignmentRequest_ThrowsArgumentNullException_WhenCountryIsoCodeIsNull()
    {
        // Arrange
        var consignment = new Consignment
        {
            Order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry
                    {
                        DeliveryAddress = new DeliveryAddress
                        {
                            Type = "Type1",
                            CountryIsoCode = null
                        }
                    }
                }
            }
        };

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            _consignmentService.ParseConsignmentToConsignmentRequest(consignment));

        // Assert
        Assert.Equal("firstEntryCountryIsoCode", exception.ParamName);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) =>
                    state.ToString().Contains("CountryIsoCode ist null beim konvertieren der Consignment")),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //UpdateConsignmentEntryQuantitiesAsync
    [Fact]
    public async Task UpdateConsignmentEntryQuantitiesAsync_OrderIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        Order? order = null;
        var returnEntry = new ReturnEntry { OrderEntryNumber = 1, Quantity = 10 };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry));
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentEntryQuantitiesAsync_ReturnEntryIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var order = new Order { Consignments = new List<Consignment>() };
        ReturnEntry returnEntry = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry));
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentEntryQuantitiesAsync_ValidOrderAndReturnEntry_UpdatesQuantities()
    {
        // Arrange
        var consignmentEntry = new ConsignmentEntry
            { OrderEntryNumber = 1, Quantity = 10, CancelledOrReturnedQuantity = 2 };
        var consignment = new Consignment { ConsignmentEntries = new List<ConsignmentEntry> { consignmentEntry } };
        var order = new Order { Consignments = new List<Consignment> { consignment } };
        var returnEntry = new ReturnEntry { OrderEntryNumber = 1, Quantity = 5 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentEntryAsync(It.IsAny<ConsignmentEntry>()))
            .Returns(Task.CompletedTask);

        // Act
        await _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry);

        // Assert
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentEntryAsync(It.Is<ConsignmentEntry>(ce =>
                ce == consignmentEntry && ce.CancelledOrReturnedQuantity == 7)), Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentEntryQuantitiesAsync_RepositoryException_Throws()
    {
        // Arrange
        var consignmentEntry = new ConsignmentEntry
            { OrderEntryNumber = 1, Quantity = 10, CancelledOrReturnedQuantity = 2 };
        var consignment = new Consignment { ConsignmentEntries = new List<ConsignmentEntry> { consignmentEntry } };
        var order = new Order { Consignments = new List<Consignment> { consignment } };
        var returnEntry = new ReturnEntry { OrderEntryNumber = 1, Quantity = 5 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentEntryAsync(It.IsAny<ConsignmentEntry>()))
            .ThrowsAsync(new RepositoryException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry));
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentEntryQuantitiesAsync_UnexpectedException_ThrowsConsignmentServiceException()
    {
        // Arrange
        var consignmentEntry = new ConsignmentEntry
            { OrderEntryNumber = 1, Quantity = 10, CancelledOrReturnedQuantity = 2 };
        var consignment = new Consignment { ConsignmentEntries = new List<ConsignmentEntry> { consignmentEntry } };
        var order = new Order { Consignments = new List<Consignment> { consignment } };
        var returnEntry = new ReturnEntry { OrderEntryNumber = 1, Quantity = 5 };

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentEntryAsync(It.IsAny<ConsignmentEntry>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.UpdateConsignmentEntryQuantitiesAsync(order, returnEntry));

        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    //GetConsignmentsWithStatusShippedAsync
    [Fact]
    public async Task GetConsignmentsWithStatusShippedAsync_ReturnsConsignments()
    {
        // Arrange
        var expectedConsignments = new List<Consignment>
        {
            new Consignment { Id = 1, Status = "Shipped" },
            new Consignment { Id = 2, Status = "Shipped" }
        };

        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentsWithStatusShippedAsync())
            .ReturnsAsync(expectedConsignments);

        // Act
        var result = await _consignmentService.GetConsignmentsWithStatusShippedAsync();

        // Assert
        Assert.Equal(expectedConsignments, result);
    }

    [Fact]
    public async Task GetConsignmentsWithStatusShippedAsync_RepositoryException_ReturnsEmptyList()
    {
        // Arrange
        var repositoryException = new RepositoryException("Repository error");
        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentsWithStatusShippedAsync())
            .ThrowsAsync(repositoryException);

        // Act
        var result = await _consignmentService.GetConsignmentsWithStatusShippedAsync();

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<RepositoryException>(ex => ex == repositoryException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }


    [Fact]
    public async Task GetConsignmentsWithStatusShippedAsync_GetConsignmentsWithStatusShippedException_ReturnsEmptyList()
    {
        // Arrange
        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentsWithStatusShippedAsync())
            .ThrowsAsync(new GetConsignmentsWithStatusShippedException("Unexpected error"));

        // Act
        var result = await _consignmentService.GetConsignmentsWithStatusShippedAsync();

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
}