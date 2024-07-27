using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;
using Shared.Services;
using Xunit;

namespace Shared.Tests.Services
{
    public class DpdTrackingDataServiceTests
    {
        private readonly Mock<ILogger<DpdTrackingDataService>> _loggerMock;
        private readonly Mock<IShippedOrdersProcessingService> _shippedOrdersProcessingServiceMock;
        private readonly Mock<IConsignmentService> _consignmentServiceMock;
        private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
        private readonly DpdTrackingDataService _dpdTrackingDataService;

        public DpdTrackingDataServiceTests()
        {
            _loggerMock = new Mock<ILogger<DpdTrackingDataService>>();
            _shippedOrdersProcessingServiceMock = new Mock<IShippedOrdersProcessingService>();
            _consignmentServiceMock = new Mock<IConsignmentService>();
            _oAuthClientServiceMock = new Mock<IOAuthClientService>();
            _dpdTrackingDataService = new DpdTrackingDataService(
                _loggerMock.Object,
                _shippedOrdersProcessingServiceMock.Object,
                _consignmentServiceMock.Object,
                _oAuthClientServiceMock.Object);
        }

        [Fact]
        public async Task ProcessTrackingData_ThrowsArgumentNullException_WhenPnrIsNull()
        {
            // Arrange
            var trackingData = new TrackingData { pnr = null, status = "delivered" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dpdTrackingDataService.ProcessTrackingData(trackingData));
        }

        [Fact]
        public async Task ProcessTrackingData_ThrowsArgumentNullException_WhenStatusIsNull()
        {
            // Arrange
            var trackingData = new TrackingData { pnr = "tracking1", status = null };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _dpdTrackingDataService.ProcessTrackingData(trackingData));
        }

        [Fact]
        public async Task ProcessTrackingData_IgnoresNonexistentConsignment()
        {
            // Arrange
            var trackingData = new TrackingData { pnr = "tracking1", status = "delivered" };
            _consignmentServiceMock.Setup(service => service.GetShippedConsignmentByTrackingIdAsync(trackingData.pnr))
                .ReturnsAsync((Consignment?)null);

            // Act
            await _dpdTrackingDataService.ProcessTrackingData(trackingData);

            // Assert
            _consignmentServiceMock.Verify(service => service.UpdateDpdConsignmentStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _oAuthClientServiceMock.Verify(service => service.ReportConsignmentDeliveryAsync(It.IsAny<Consignment>(), 0), Times.Never);
            _shippedOrdersProcessingServiceMock.Verify(service => service.CheckAndProcessShippedOrders(), Times.Never);
        }

        [Fact]
        public async Task ProcessTrackingData_UpdatesConsignmentStatusAndReportsDelivery()
        {
            // Arrange
            var trackingData = new TrackingData { pnr = "tracking1", status = SharedStatus.delivery_customer };
            var consignment = new Consignment { Id = 1, TrackingId = trackingData.pnr };
            _consignmentServiceMock.Setup(service => service.GetShippedConsignmentByTrackingIdAsync(trackingData.pnr))
                .ReturnsAsync(consignment);
            _consignmentServiceMock.Setup(service => service.UpdateDpdConsignmentStatusAsync(trackingData.status, trackingData.pnr))
                .ReturnsAsync(true);

            // Act
            await _dpdTrackingDataService.ProcessTrackingData(trackingData);

            // Assert
            _consignmentServiceMock.Verify(service => service.UpdateDpdConsignmentStatusAsync(trackingData.status, trackingData.pnr), Times.Once);
            _oAuthClientServiceMock.Verify(service => service.ReportConsignmentDeliveryAsync(consignment, 0), Times.Once);
            _shippedOrdersProcessingServiceMock.Verify(service => service.CheckAndProcessShippedOrders(), Times.Once);
        }

        [Fact]
        public async Task ProcessTrackingData_LogsWarningOnException()
        {
            // Arrange
            var trackingData = new TrackingData { pnr = "tracking1", status = SharedStatus.delivery_customer };
            var consignment = new Consignment { Id = 1, TrackingId = trackingData.pnr };
            _consignmentServiceMock.Setup(service => service.GetShippedConsignmentByTrackingIdAsync(trackingData.pnr))
                .ReturnsAsync(consignment);
            _consignmentServiceMock.Setup(service => service.UpdateDpdConsignmentStatusAsync(trackingData.status, trackingData.pnr))
                .ReturnsAsync(true);
            _oAuthClientServiceMock.Setup(service => service.ReportConsignmentDeliveryAsync(consignment, 0))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            await _dpdTrackingDataService.ProcessTrackingData(trackingData);

            // Assert
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
