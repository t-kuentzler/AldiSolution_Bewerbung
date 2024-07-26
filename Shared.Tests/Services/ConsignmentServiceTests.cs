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
}