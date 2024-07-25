using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;
using Shared.Services;

namespace Shared.Tests.Services;

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
                _loggerMock.Object
            );
        }

        
}