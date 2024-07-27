using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts;
using Shared.Entities;
using Shared.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Constants;
using Xunit;

namespace Shared.Tests.Services
{
    public class DhlTrackingStatusServiceTests
    {
        private readonly Mock<ILogger<DhlTrackingStatusService>> _loggerMock;
        private readonly Mock<IDhlApiService> _dhlApiServiceMock;
        private readonly Mock<IAccessTokenService> _accessTokenServiceMock;
        private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
        private readonly Mock<IConsignmentService> _consignmentServiceMock;
        private readonly DhlTrackingStatusService _dhlTrackingStatusService;

        public DhlTrackingStatusServiceTests()
        {
            _loggerMock = new Mock<ILogger<DhlTrackingStatusService>>();
            _dhlApiServiceMock = new Mock<IDhlApiService>();
            _accessTokenServiceMock = new Mock<IAccessTokenService>();
            _oAuthClientServiceMock = new Mock<IOAuthClientService>();
            _consignmentServiceMock = new Mock<IConsignmentService>();
            _dhlTrackingStatusService = new DhlTrackingStatusService(
                _loggerMock.Object,
                _dhlApiServiceMock.Object,
                _accessTokenServiceMock.Object,
                _oAuthClientServiceMock.Object,
                _consignmentServiceMock.Object);
        }

        [Fact]
        public async Task ReadAndUpdateTrackingStatusAsync_NoConsignmentsWithShippedStatus_LogsInformation()
        {
            // Arrange
            _consignmentServiceMock.Setup(service => service.GetConsignmentsWithStatusShippedAsync())
                .ReturnsAsync(new List<Consignment>());

            // Act
            await _dhlTrackingStatusService.ReadAndUpdateTrackingStatusAsync();

            // Assert
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ReadAndUpdateTrackingStatusAsync_UpdatesConsignmentStatus()
        {
            // Arrange
            var consignment = new Consignment
            {
                Id = 1,
                TrackingId = "tracking1",
                Status = SharedStatus.Shipped
            };
            var consignments = new List<Consignment> { consignment };
            _consignmentServiceMock.Setup(service => service.GetConsignmentsWithStatusShippedAsync())
                .ReturnsAsync(consignments);
            _dhlApiServiceMock.Setup(service => service.GetTrackingStatusFromApiAsync(consignment.TrackingId))
                .ReturnsAsync(SharedStatus.Delivered);
            _consignmentServiceMock.Setup(service => service.UpdateConsignmentStatusByConsignmentIdAsync(SharedStatus.Delivered, consignment.Id))
                .ReturnsAsync(true);
            _consignmentServiceMock.Setup(service => service.GetConsignmentByConsignmentIdAsync(consignment.Id))
                .ReturnsAsync(consignment);

            // Act
            await _dhlTrackingStatusService.ReadAndUpdateTrackingStatusAsync();

            // Assert
            _consignmentServiceMock.Verify(service => service.UpdateConsignmentStatusByConsignmentIdAsync(SharedStatus.Delivered, consignment.Id), Times.Once);
            _oAuthClientServiceMock.Verify(service => service.ReportConsignmentDeliveryAsync(consignment, 0), Times.Once);
        }


        [Fact]
        public async Task ReadAndUpdateTrackingStatusAsync_DoesNotReportDelivery_WhenUpdateFails()
        {
            // Arrange
            var consignment = new Consignment
            {
                Id = 1,
                TrackingId = "tracking1",
                Status = SharedStatus.Shipped
            };
            var consignments = new List<Consignment> { consignment };
            _consignmentServiceMock.Setup(service => service.GetConsignmentsWithStatusShippedAsync())
                .ReturnsAsync(consignments);
            _dhlApiServiceMock.Setup(service => service.GetTrackingStatusFromApiAsync(consignment.TrackingId))
                .ReturnsAsync(SharedStatus.Delivered);
            _consignmentServiceMock.Setup(service => service.UpdateConsignmentStatusByConsignmentIdAsync(SharedStatus.Delivered, consignment.Id))
                .ReturnsAsync(false);

            // Act
            await _dhlTrackingStatusService.ReadAndUpdateTrackingStatusAsync();

            // Assert
            _oAuthClientServiceMock.Verify(service => service.ReportConsignmentDeliveryAsync(It.IsAny<Consignment>(), 5), Times.Never);
        }

        [Fact]
        public async Task ReadAndUpdateTrackingStatusAsync_SkipsReporting_WhenConsignmentIsNull()
        {
            // Arrange
            var consignment = new Consignment
            {
                Id = 1,
                TrackingId = "tracking1",
                Status = SharedStatus.Shipped
            };
            var consignments = new List<Consignment> { consignment };
            _consignmentServiceMock.Setup(service => service.GetConsignmentsWithStatusShippedAsync())
                .ReturnsAsync(consignments);
            _dhlApiServiceMock.Setup(service => service.GetTrackingStatusFromApiAsync(consignment.TrackingId))
                .ReturnsAsync(SharedStatus.Delivered);
            _consignmentServiceMock.Setup(service => service.UpdateConsignmentStatusByConsignmentIdAsync(SharedStatus.Delivered, consignment.Id))
                .ReturnsAsync(true);
            _consignmentServiceMock.Setup(service => service.GetConsignmentByConsignmentIdAsync(consignment.Id))
                .ReturnsAsync((Consignment?)null);

            // Act
            await _dhlTrackingStatusService.ReadAndUpdateTrackingStatusAsync();

            // Assert
            _oAuthClientServiceMock.Verify(service => service.ReportConsignmentDeliveryAsync(It.IsAny<Consignment>(), 5), Times.Never);
        }
    }
}
