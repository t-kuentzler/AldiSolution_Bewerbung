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