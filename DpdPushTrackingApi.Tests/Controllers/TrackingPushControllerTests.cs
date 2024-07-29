using DpdPushTrackingApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Models;

namespace DpdPushTrackingApi.Tests.Controllers;

public class TrackingPushControllerTests
{
    private readonly Mock<IDpdTrackingDataService> _dpdTrackingDataServiceMock;
    private readonly Mock<ILogger<TrackingPushController>> _loggerMock;
    private readonly TrackingPushController _controller;

    public TrackingPushControllerTests()
    {
        _dpdTrackingDataServiceMock = new Mock<IDpdTrackingDataService>();
        _loggerMock = new Mock<ILogger<TrackingPushController>>();
        _controller = new TrackingPushController(_dpdTrackingDataServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Index_WithoutPushId_ReturnsBadRequest()
    {
        // Arrange
        var trackingData = new TrackingData { pushid = null };

        // Act
        var result = await _controller.Index(trackingData);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Es fehlt die erforderliche pushId.", badRequestResult.Value);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Anfrage ohne pushId erhalten.")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_WithValidPushId_ReturnsOk()
    {
        // Arrange
        var trackingData = new TrackingData { pushid = "validPushId" };
        _dpdTrackingDataServiceMock.Setup(service => service.ProcessTrackingData(trackingData))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Index(trackingData);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var expectedResponseXml = $"<push><pushid>{trackingData.pushid}</pushid><status>OK</status></push>";
        Assert.Equal(expectedResponseXml, okResult.Value);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Empfangene Tracking-Daten")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        var trackingData = new TrackingData { pushid = "validPushId" };
        _dpdTrackingDataServiceMock.Setup(service => service.ProcessTrackingData(trackingData))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.Index(trackingData);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        Assert.Equal("Ein interner Fehler ist aufgetreten.", statusCodeResult.Value);
        _loggerMock.Verify(
            logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fehler bei der Verarbeitung der Tracking-Daten")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}