using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
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
    public class ConsignmentProcessingServiceTests
    {
        private readonly Mock<IConsignmentService> _consignmentServiceMock;
        private readonly Mock<ICsvFileService> _csvFileServiceMock;
        private readonly Mock<IOAuthClientService> _oAuthClientServiceMock;
        private readonly Mock<IValidator<Consignment>> _consignmentValidatorMock;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<ILogger<ConsignmentProcessingService>> _loggerMock;
        private readonly ConsignmentProcessingService _consignmentProcessingService;

        public ConsignmentProcessingServiceTests()
        {
            _consignmentServiceMock = new Mock<IConsignmentService>();
            _csvFileServiceMock = new Mock<ICsvFileService>();
            _oAuthClientServiceMock = new Mock<IOAuthClientService>();
            _consignmentValidatorMock = new Mock<IValidator<Consignment>>();
            _orderServiceMock = new Mock<IOrderService>();
            _loggerMock = new Mock<ILogger<ConsignmentProcessingService>>();
            _consignmentProcessingService = new ConsignmentProcessingService(
                _consignmentServiceMock.Object,
                _csvFileServiceMock.Object,
                _oAuthClientServiceMock.Object,
                _consignmentValidatorMock.Object,
                _orderServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ReadAndSaveConsignmentsAsync_Success()
        {
            // Arrange
            var consignmentsFromCsv = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv
                {
                    paket = "Paket1",
                    kdnr = "KDN1",
                    datum_druck = "2024-07-26",
                    lieferschein = "Lief1",
                    nve_nr = "NVE1",
                    kontrakt_nr_kunde = "Kontrakt1",
                    name1 = "Name1",
                    strasse = "Strasse1",
                    nation = "DE",
                    plz = "12345",
                    ort = "Ort1",
                    verpackungs_nr = "Verpackung1",
                    artikelnummer = "Artikel1",
                    menge = "10"
                }
            };
            var consignments = new List<Consignment>
            {
                new Consignment { OrderCode = "Order1" }
            };

            _csvFileServiceMock.Setup(s => s.GetConsignmentsFromCsvFiles()).Returns(consignmentsFromCsv);
            _csvFileServiceMock.Setup(s => s.ParseConsignmentsFromCsvToConsignments(consignmentsFromCsv)).ReturnsAsync(consignments);
            _consignmentServiceMock.Setup(s => s.SaveConsignmentAsync(It.IsAny<Consignment>())).ReturnsAsync((true, 1));
            _oAuthClientServiceMock.Setup(s => s.CreateApiConsignmentAsync(It.IsAny<List<ConsignmentRequest>>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync(new ConsignmentListResponse());
            _orderServiceMock.Setup(s => s.GetOrderStatusByOrderCodeAsync(It.IsAny<string>())).ReturnsAsync(SharedStatus.InProgress);

            // Act
            await _consignmentProcessingService.ReadAndSaveConsignmentsAsync();

            // Assert
            _csvFileServiceMock.Verify(s => s.MoveCsvFilesToArchiv(), Times.Once);
            _consignmentServiceMock.Verify(s => s.SaveConsignmentAsync(It.IsAny<Consignment>()), Times.Once);
            _oAuthClientServiceMock.Verify(s => s.CreateApiConsignmentAsync(It.IsAny<List<ConsignmentRequest>>(), It.IsAny<string>(), 0), Times.Once);
            _orderServiceMock.Verify(s => s.UpdateSingleOrderStatusInDatabaseAsync(It.IsAny<string>(), SharedStatus.Shipped), Times.Once);
        }

        [Fact]
        public async Task ReadAndSaveConsignmentsAsync_HandlesCsvFileError()
        {
            // Arrange
            _csvFileServiceMock.Setup(s => s.GetConsignmentsFromCsvFiles()).Throws(new Exception("CSV error"));

            // Act
            await _consignmentProcessingService.ReadAndSaveConsignmentsAsync();

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ReadAndSaveConsignmentsAsync_HandlesConsignmentServiceError()
        {
            // Arrange
            var consignmentsFromCsv = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv
                {
                    paket = "Paket1",
                    kdnr = "KDN1",
                    datum_druck = "2024-07-26",
                    lieferschein = "Lief1",
                    nve_nr = "NVE1",
                    kontrakt_nr_kunde = "Kontrakt1",
                    name1 = "Name1",
                    strasse = "Strasse1",
                    nation = "DE",
                    plz = "12345",
                    ort = "Ort1",
                    verpackungs_nr = "Verpackung1",
                    artikelnummer = "Artikel1",
                    menge = "10"
                }
            };
            var consignments = new List<Consignment>
            {
                new Consignment { OrderCode = "Order1" }
            };

            _csvFileServiceMock.Setup(s => s.GetConsignmentsFromCsvFiles()).Returns(consignmentsFromCsv);
            _csvFileServiceMock.Setup(s => s.ParseConsignmentsFromCsvToConsignments(consignmentsFromCsv)).ReturnsAsync(consignments);
            _consignmentServiceMock.Setup(s => s.SaveConsignmentAsync(It.IsAny<Consignment>())).ReturnsAsync((false, 0));

            // Act
            await _consignmentProcessingService.ReadAndSaveConsignmentsAsync();

            // Assert
            _consignmentServiceMock.Verify(s => s.SaveConsignmentAsync(It.IsAny<Consignment>()), Times.Once);
            _oAuthClientServiceMock.Verify(s => s.CreateApiConsignmentAsync(It.IsAny<List<ConsignmentRequest>>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ReadAndSaveConsignmentsAsync_HandlesApiClientError()
        {
            // Arrange
            var consignmentsFromCsv = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv
                {
                    paket = "Paket1",
                    kdnr = "KDN1",
                    datum_druck = "2024-07-26",
                    lieferschein = "Lief1",
                    nve_nr = "NVE1",
                    kontrakt_nr_kunde = "Kontrakt1",
                    name1 = "Name1",
                    strasse = "Strasse1",
                    nation = "DE",
                    plz = "12345",
                    ort = "Ort1",
                    verpackungs_nr = "Verpackung1",
                    artikelnummer = "Artikel1",
                    menge = "10"
                }
            };
            var consignments = new List<Consignment>
            {
                new Consignment { OrderCode = "Order1" }
            };

            _csvFileServiceMock.Setup(s => s.GetConsignmentsFromCsvFiles()).Returns(consignmentsFromCsv);
            _csvFileServiceMock.Setup(s => s.ParseConsignmentsFromCsvToConsignments(consignmentsFromCsv)).ReturnsAsync(consignments);
            _consignmentServiceMock.Setup(s => s.SaveConsignmentAsync(It.IsAny<Consignment>())).ReturnsAsync((true, 1));
            _oAuthClientServiceMock.Setup(s => s.CreateApiConsignmentAsync(It.IsAny<List<ConsignmentRequest>>(), It.IsAny<string>(), It.IsAny<int>())).Throws(new Exception("API error"));

            // Act
            await _consignmentProcessingService.ReadAndSaveConsignmentsAsync();

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        [Fact]
        public async Task ReadAndSaveConsignmentsAsync_NoConsignments_DoesNotCallApi()
        {
            // Arrange
            _csvFileServiceMock.Setup(s => s.GetConsignmentsFromCsvFiles()).Returns(new List<ConsignmentFromCsv>());
            _csvFileServiceMock.Setup(s => s.ParseConsignmentsFromCsvToConsignments(It.IsAny<List<ConsignmentFromCsv>>())).ReturnsAsync(new List<Consignment>());

            // Act
            await _consignmentProcessingService.ReadAndSaveConsignmentsAsync();

            // Assert
            _oAuthClientServiceMock.Verify(s => s.CreateApiConsignmentAsync(It.IsAny<List<ConsignmentRequest>>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
            _orderServiceMock.Verify(s => s.UpdateSingleOrderStatusInDatabaseAsync(It.IsAny<string>(), SharedStatus.Shipped), Times.Never);
        }

        
    }
}
