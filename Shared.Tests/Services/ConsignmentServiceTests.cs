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

    //UpdateConsignmentStatusByConsignmentIdAsync
    [Fact]
    public async Task UpdateConsignmentStatusByConsignmentIdAsync_NewStatusIsDelivered_ReturnsTrue()
    {
        // Arrange
        var newStatus = SharedStatus.delivered;
        var consignmentId = 1;

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByIdAsync(consignmentId, SharedStatus.Delivered))
            .ReturnsAsync(true);

        // Act
        var result = await _consignmentService.UpdateConsignmentStatusByConsignmentIdAsync(newStatus, consignmentId);

        // Assert
        Assert.True(result);
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentStatusByIdAsync(consignmentId, SharedStatus.Delivered), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentStatusByConsignmentIdAsync_NewStatusIsNotDelivered_ReturnsFalse()
    {
        // Arrange
        var newStatus = "NotDelivered";
        var consignmentId = 1;

        // Act
        var result = await _consignmentService.UpdateConsignmentStatusByConsignmentIdAsync(newStatus, consignmentId);

        // Assert
        Assert.False(result);
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentStatusByIdAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateConsignmentStatusByConsignmentIdAsync_RepositoryException_ReturnsFalse()
    {
        // Arrange
        var repositoryException = new RepositoryException("Repository error");
        var newStatus = SharedStatus.delivered;
        var consignmentId = 1;

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByIdAsync(consignmentId, SharedStatus.Delivered))
            .ThrowsAsync(repositoryException);

        // Act
        var result = await _consignmentService.UpdateConsignmentStatusByConsignmentIdAsync(newStatus, consignmentId);
        // Assert
        Assert.False(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<RepositoryException>(ex => ex == repositoryException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateConsignmentStatusByConsignmentIdAsync_UnexpectedException_ReturnsFalse()
    {
        // Arrange
        var repositoryException = new Exception("unexpected error");
        var newStatus = SharedStatus.delivered;
        var consignmentId = 1;

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByIdAsync(consignmentId, SharedStatus.Delivered))
            .ThrowsAsync(repositoryException);

        // Act
        var result = await _consignmentService.UpdateConsignmentStatusByConsignmentIdAsync(newStatus, consignmentId);
        // Assert
        Assert.False(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => ex == repositoryException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    //GetConsignmentByConsignmentIdAsync
    [Fact]
    public async Task GetConsignmentByConsignmentIdAsync_ReturnsConsignment()
    {
        // Arrange
        var consignmentId = 1;
        var expectedConsignment = new Consignment { Id = consignmentId };

        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentByConsignmentIdAsync(consignmentId))
            .ReturnsAsync(expectedConsignment);

        // Act
        var result = await _consignmentService.GetConsignmentByConsignmentIdAsync(consignmentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConsignment, result);
    }

    [Fact]
    public async Task GetConsignmentByConsignmentIdAsync_RepositoryException_Throws()
    {
        // Arrange
        var consignmentId = 1;
        var repositoryException = new RepositoryException("Repository error");

        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentByConsignmentIdAsync(consignmentId))
            .ThrowsAsync(repositoryException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RepositoryException>(() =>
            _consignmentService.GetConsignmentByConsignmentIdAsync(consignmentId));

        Assert.Equal(repositoryException, exception);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                repositoryException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetConsignmentByConsignmentIdAsync_UnexpectedException_ThrowsConsignmentServiceException()
    {
        // Arrange
        var consignmentId = 1;
        var unexpectedException = new Exception("Unexpected error");

        _consignmentRepositoryMock
            .Setup(repo => repo.GetConsignmentByConsignmentIdAsync(consignmentId))
            .ThrowsAsync(unexpectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.GetConsignmentByConsignmentIdAsync(consignmentId));

        Assert.Equal(
            $"Es ist ein unerwarteter Fehler beim abrufen des Consignment mit der Consignment Id '{consignmentId}' aufgetreten.",
            exception.Message);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                unexpectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    //GetShippedConsignmentByTrackingIdAsync
    [Fact]
    public async Task GetShippedConsignmentByTrackingIdAsync_ReturnsConsignment()
    {
        // Arrange
        var trackingId = "tracking123";
        var expectedConsignment = new Consignment { TrackingId = trackingId };

        _consignmentRepositoryMock
            .Setup(repo => repo.GetShippedConsignmentByTrackingIdAsync(trackingId))
            .ReturnsAsync(expectedConsignment);

        // Act
        var result = await _consignmentService.GetShippedConsignmentByTrackingIdAsync(trackingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedConsignment, result);
    }

    [Fact]
    public async Task GetShippedConsignmentByTrackingIdAsync_RepositoryException_ReturnsEmptyConsignment()
    {
        // Arrange
        var trackingId = "tracking123";
        var repositoryException = new RepositoryException("Repository error");

        _consignmentRepositoryMock
            .Setup(repo => repo.GetShippedConsignmentByTrackingIdAsync(trackingId))
            .ThrowsAsync(repositoryException);

        // Act
        var result = await _consignmentService.GetShippedConsignmentByTrackingIdAsync(trackingId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Consignment>(result);
        Assert.Empty(result.ConsignmentEntries);
        Assert.Equal(0, result.Id);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                repositoryException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetShippedConsignmentByTrackingIdAsync_UnexpectedException_ReturnsEmptyConsignment()
    {
        // Arrange
        var trackingId = "tracking123";
        var unexpectedException = new Exception("Unexpected error");

        _consignmentRepositoryMock
            .Setup(repo => repo.GetShippedConsignmentByTrackingIdAsync(trackingId))
            .ThrowsAsync(unexpectedException);

        // Act
        var result = await _consignmentService.GetShippedConsignmentByTrackingIdAsync(trackingId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Consignment>(result);
        Assert.Empty(result.ConsignmentEntries);
        Assert.Equal(0, result.Id);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                unexpectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    //UpdateDpdConsignmentStatusAsync
    [Fact]
    public async Task UpdateDpdConsignmentStatusAsync_NewStatusIsDeliveryCustomer_ReturnsTrue()
    {
        // Arrange
        var newStatus = SharedStatus.delivery_customer;
        var trackingId = "tracking123";

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _consignmentService.UpdateDpdConsignmentStatusAsync(newStatus, trackingId);

        // Assert
        Assert.True(result);
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDpdConsignmentStatusAsync_NewStatusIsPickupByConsignee_ReturnsTrue()
    {
        // Arrange
        var newStatus = SharedStatus.pickup_by_consignee;
        var trackingId = "tracking123";

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _consignmentService.UpdateDpdConsignmentStatusAsync(newStatus, trackingId);

        // Assert
        Assert.True(result);
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId), Times.Once);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDpdConsignmentStatusAsync_NewStatusIsNotValid_ReturnsFalse()
    {
        // Arrange
        var newStatus = "invalid_status";
        var trackingId = "tracking123";

        // Act
        var result = await _consignmentService.UpdateDpdConsignmentStatusAsync(newStatus, trackingId);

        // Assert
        Assert.False(result);
        _consignmentRepositoryMock.Verify(
            repo => repo.UpdateConsignmentStatusByTrackingIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateDpdConsignmentStatusAsync_RepositoryException_ReturnsFalse()
    {
        // Arrange
        var newStatus = SharedStatus.delivery_customer;
        var trackingId = "tracking123";
        var repositoryException = new RepositoryException("Repository error");

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId))
            .ThrowsAsync(repositoryException);

        // Act
        var result = await _consignmentService.UpdateDpdConsignmentStatusAsync(newStatus, trackingId);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                repositoryException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDpdConsignmentStatusAsync_UnexpectedException_ReturnsFalse()
    {
        // Arrange
        var newStatus = SharedStatus.delivery_customer;
        var trackingId = "tracking123";
        var unexpectedException = new Exception("Unexpected error");

        _consignmentRepositoryMock
            .Setup(repo => repo.UpdateConsignmentStatusByTrackingIdAsync(SharedStatus.Delivered, trackingId))
            .ThrowsAsync(unexpectedException);

        // Act
        var result = await _consignmentService.UpdateDpdConsignmentStatusAsync(newStatus, trackingId);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                unexpectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    //UpdateConsignmentStatusAsync
    [Fact]
    public async Task UpdateConsignmentStatusAsync_UpdatesStatusAndLogsInfo_WhenSuccessful()
    {
        // Arrange
        var consignment = new Consignment { Id = 1, Status = "Pending" };
        var newStatus = "Shipped";
        _consignmentRepositoryMock.Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _consignmentService.UpdateConsignmentStatusAsync(newStatus, consignment);

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
    public async Task UpdateConsignmentStatusAsync_ThrowsRepositoryException_WhenRepositoryThrowsRepositoryException()
    {
        // Arrange
        var consignment = new Consignment { Id = 1, Status = "Pending" };
        var newStatus = "Shipped";

        _consignmentRepositoryMock.Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .ThrowsAsync(new RepositoryException("Test exception"));

        // Act
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _consignmentService.UpdateConsignmentStatusAsync(newStatus, consignment));

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
    public async Task UpdateConsignmentStatusAsync_ThrowsConsignmentServiceException_WhenRepositoryThrowsException()
    {
        // Arrange
        var consignment = new Consignment { Id = 1, Status = "Pending" };
        var newStatus = "Shipped";

        _consignmentRepositoryMock.Setup(repo => repo.UpdateConsignmentAsync(consignment))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.UpdateConsignmentStatusAsync(newStatus, consignment));

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
    
    //SearchConsignmentsAsync
    [Fact]
    public async Task SearchConsignmentsAsync_ReturnsEmptyList_WhenSearchTermIsNullOrWhiteSpace()
    {
        // Arrange
        var searchTerm = new SearchTerm
        {
            value = " "
        };
        string status = "AnyStatus";

        // Act
        var result = await _consignmentService.SearchConsignmentsAsync(searchTerm, status);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchConsignmentsAsync_ThrowsArgumentException_WhenStatusIsNull()
    {
        // Arrange
        var searchTerm = new SearchTerm
        {
            value = "valid"
        };
        string status = null;


        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _consignmentService.SearchConsignmentsAsync(searchTerm, status));

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
    public async Task SearchConsignmentsAsync_ThrowsValidationException_WhenSearchTermIsInvalid()
    {
        // Arrange
        var searchTerm = new SearchTerm
        {
            value = "1111111111111111111111111111111" //Mehr als 30 Zeichen
        };
        var status = "anyStatus";

        _searchTermValidatorMock
            .Setup(validator => validator.ValidateAndThrowAsync(searchTerm))
            .ThrowsAsync(new ValidationException("Validation failed"));


        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _consignmentService.SearchConsignmentsAsync(searchTerm, status));

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
    public async Task SearchConsignmentsAsync_ThrowsRepositoryException_WhenRepositoryThrowsRepositoryException()
    {
        // Arrange
        var searchTerm = new SearchTerm
        {
            value = "Test"
        };
        string status = "anyStatus";

        _consignmentRepositoryMock.Setup(repo => repo.SearchShippedConsignmentsAsync(searchTerm))
            .ThrowsAsync(new RepositoryException("Test exception"));

        //Act
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _consignmentService.SearchConsignmentsAsync(searchTerm, status));

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
    public async Task SearchConsignmentsAsync_ThrowsConsignmentServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var searchTerm = new SearchTerm
        {
            value = "Test"
        };
        string status = "anyStatus";

        _consignmentRepositoryMock.Setup(repo => repo.SearchShippedConsignmentsAsync(searchTerm))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var exception = await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.SearchConsignmentsAsync(searchTerm, status));

        // Assert
        Assert.IsType<Exception?>(exception.InnerException);
        Assert.Equal("Unexpected error", exception.InnerException?.Message);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }
    
    //GetAllConsignmentsByStatusAsync
    [Fact]
    public async Task GetAllConsignmentsByStatusAsync_ReturnsConsignments_WhenStatusIsValid()
    {
        // Arrange
        var status = "DELIVERED";
        var consignments = new List<Consignment>
        {
            new Consignment()
            {
                Id = 1,
                ConsignmentEntries = new List<ConsignmentEntry>()
                {
                }
            },
            new Consignment()
            {
                Id = 2,
                ConsignmentEntries = new List<ConsignmentEntry>()
                {
                }
            },
        };

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentsWithStatusAsync(status))
            .ReturnsAsync(consignments);

        // Act
        var result = await _consignmentService.GetAllConsignmentsByStatusAsync(status);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Erwarten Sie die gleiche Anzahl von Orders, die Sie im Setup festgelegt haben
        _consignmentRepositoryMock.Verify(repo => repo.GetConsignmentsWithStatusAsync(status), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAllConsignmentsByStatusAsync_ThrowsArgumentNullException_WhenStatusIsNullOrEmpty(string status)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _consignmentService.GetAllConsignmentsByStatusAsync(status));

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
    public async Task GetAllConsignmentsByStatusAsync_ThrowsAndLogsRepositoryException_WhenRepositoryExceptionOccurs()
    {
        // Arrange
        var status = "SHIPPED";

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentsWithStatusAsync(status))
            .ThrowsAsync(new RepositoryException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryException>(() =>
            _consignmentService.GetAllConsignmentsByStatusAsync(status));

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
    public async Task GetAllConsignmentsByStatusAsync_ThrowsConsignmentServiceException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var status = "PROCESSING";

        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentsWithStatusAsync(status))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        await Assert.ThrowsAsync<ConsignmentServiceException>(() =>
            _consignmentService.GetAllConsignmentsByStatusAsync(status));

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
    public async Task GetAllConsignmentsByStatusAsync_ReturnsEmptyList_WhenNoConsignmentsFound()
    {
        // Arrange
        var status = "CANCELLED";
        _consignmentRepositoryMock.Setup(repo => repo.GetConsignmentsWithStatusAsync(status))
            .ReturnsAsync(new List<Consignment>());

        // Act
        var result = await _consignmentService.GetAllConsignmentsByStatusAsync(status);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
    
    //AreAllConsignmentsCancelled
    [Fact]
        public void AreAllConsignmentsCancelled_OrderIsNull_ThrowsOrderIsNullException()
        {
            // Arrange
            Order? order = null;

            // Act & Assert
            Assert.Throws<OrderIsNullException>(() => _consignmentService.AreAllConsignmentsCancelled(order));
            
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
        public void AreAllConsignmentsCancelled_AllConsignmentsCancelled_ReturnsTrue()
        {
            // Arrange
            var order = new Order
            {
                Consignments = new List<Consignment>
                {
                    new Consignment { Status = SharedStatus.Cancelled },
                    new Consignment { Status = SharedStatus.Cancelled }
                }
            };

            // Act
            var result = _consignmentService.AreAllConsignmentsCancelled(order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AreAllConsignmentsCancelled_NotAllConsignmentsCancelled_ReturnsFalse()
        {
            // Arrange
            var order = new Order
            {
                Consignments = new List<Consignment>
                {
                    new Consignment { Status = SharedStatus.Cancelled },
                    new Consignment { Status = SharedStatus.Delivered }
                }
            };

            // Act
            var result = _consignmentService.AreAllConsignmentsCancelled(order);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AreAllConsignmentsCancelled_EmptyConsignmentsList_ReturnsFalse()
        {
            // Arrange
            var order = new Order
            {
                Consignments = new List<Consignment>(){new Consignment()
                {
                    Status = SharedStatus.Shipped
                }}
            };

            // Act
            var result = _consignmentService.AreAllConsignmentsCancelled(order);

            // Assert
            Assert.False(result);
        }
        
        //GetConsignmentEntryByIdAsync
         [Fact]
        public async Task GetConsignmentEntryByIdAsync_InvalidId_ThrowsInvalidIdException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidIdException>(() => _consignmentService.GetConsignmentEntryByIdAsync(0));
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
        public async Task GetConsignmentEntryByIdAsync_RepositoryException_ThrowsRepositoryException()
        {
            // Arrange
            var consignmentEntryId = 1;
            var repositoryException = new RepositoryException("Repository error");

            _consignmentRepositoryMock
                .Setup(repo => repo.GetConsignmentEntryByIdAsync(consignmentEntryId))
                .ThrowsAsync(repositoryException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RepositoryException>(() => _consignmentService.GetConsignmentEntryByIdAsync(consignmentEntryId));
            Assert.Equal(repositoryException, exception);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);        }

        [Fact]
        public async Task GetConsignmentEntryByIdAsync_UnexpectedException_ThrowsConsignmentServiceException()
        {
            // Arrange
            var consignmentEntryId = 1;
            var unexpectedException = new Exception("Unexpected error");

            _consignmentRepositoryMock
                .Setup(repo => repo.GetConsignmentEntryByIdAsync(consignmentEntryId))
                .ThrowsAsync(unexpectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConsignmentServiceException>(() => _consignmentService.GetConsignmentEntryByIdAsync(consignmentEntryId));
            Assert.Equal($"Unerwarteter Fehler beim Abrufen von ConsignmentEntry mit dem Status '{consignmentEntryId}'.", exception.Message);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);        }

        [Fact]
        public async Task GetConsignmentEntryByIdAsync_ValidId_ReturnsConsignmentEntry()
        {
            // Arrange
            var consignmentEntryId = 1;
            var expectedConsignmentEntry = new ConsignmentEntry { Id = consignmentEntryId };

            _consignmentRepositoryMock
                .Setup(repo => repo.GetConsignmentEntryByIdAsync(consignmentEntryId))
                .ReturnsAsync(expectedConsignmentEntry);

            // Act
            var result = await _consignmentService.GetConsignmentEntryByIdAsync(consignmentEntryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedConsignmentEntry, result);
        }
    }
    




