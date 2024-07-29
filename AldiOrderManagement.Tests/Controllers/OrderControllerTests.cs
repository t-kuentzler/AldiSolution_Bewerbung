using AldiOrderManagement.Controllers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace AldiOrderManagement.Tests.Controllers
{
    public class OrderControllerTests
    {
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly Mock<IFileService> _fileServiceMock;
        private readonly Mock<ILogger<OrderController>> _loggerMock;
        private readonly OrderController _controller;

        public OrderControllerTests()
        {
            _orderServiceMock = new Mock<IOrderService>();
            _fileServiceMock = new Mock<IFileService>();
            _loggerMock = new Mock<ILogger<OrderController>>();
            
            _controller = new OrderController(_orderServiceMock.Object, _fileServiceMock.Object, _loggerMock.Object)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        //Index
        [Fact]
        public async Task Index_ReturnsViewWithOrders_WhenOrdersAreAvailable()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order { Id = 1, Created = DateTime.UtcNow },
                new Order { Id = 2, Created = DateTime.UtcNow.AddHours(-1) }
            };
            _orderServiceMock.Setup(s => s.GetAllOrdersByStatusAsync(SharedStatus.InProgress))
                             .ReturnsAsync(orders);

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Equal(2, model.Count);
            Assert.Equal(1, model.First().Id);
        }

        [Fact]
        public async Task Index_SetsTempDataErrorMessage_WhenOrderServiceExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "Test exception message";
            _orderServiceMock.Setup(s => s.GetAllOrdersByStatusAsync(SharedStatus.InProgress))
                             .ThrowsAsync(new OrderServiceException(exceptionMessage));

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Empty(model);
            Assert.Equal(
                $"Fehler beim Abrufen von Bestellungen mit dem Status '{SharedStatus.InProgress}': {exceptionMessage}",
                _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Index_SetsTempDataErrorMessage_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.GetAllOrdersByStatusAsync(SharedStatus.InProgress))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Empty(model);
            Assert.Equal("Es ist ein unerwarteter Fehler.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler beim abrufen der offenen Bestellungen aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //OrderDetails
        [Fact]
        public async Task OrderDetails_ReturnsViewWithOrder_WhenOrderExists()
        {
            // Arrange
            var order = new Order { Id = 1 };
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1))
                             .ReturnsAsync(order);

            // Act
            var result = await _controller.OrderDetails(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Order>(result.Model);
            Assert.Equal(order, result.Model);
        }

        [Fact]
        public async Task OrderDetails_SetsTempDataErrorMessage_WhenOrderServiceExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "Test exception message";
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(It.IsAny<int>()))
                             .ThrowsAsync(new OrderServiceException(exceptionMessage));

            // Act
            var result = await _controller.OrderDetails(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal($"Fehler beim Abrufen von Bestellungsdetails: {exceptionMessage}", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task OrderDetails_SetsTempDataErrorMessage_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(It.IsAny<int>()))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.OrderDetails(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Es ist ein unerwarteter Fehler aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler beim abrufen der Bestelldetails der Bestellung mit der Id '1' aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //OrderDetailsWithReturn
        [Fact]
        public async Task OrderDetailsWithReturn_ReturnsViewWithOrder_WhenOrderExists()
        {
            // Arrange
            var order = new Order { Id = 1 };
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1))
                             .ReturnsAsync(order);

            // Act
            var result = await _controller.OrderDetailsWithReturn(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Order>(result.Model);
            Assert.Equal(order, result.Model);
        }

        [Fact]
        public async Task OrderDetailsWithReturn_SetsTempDataErrorMessage_WhenOrderServiceExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "Test exception message";
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(It.IsAny<int>()))
                             .ThrowsAsync(new OrderServiceException(exceptionMessage));

            // Act
            var result = await _controller.OrderDetailsWithReturn(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal($"Fehler beim Abrufen von Bestellungsdetails: {exceptionMessage}", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task OrderDetailsWithReturn_SetsTempDataErrorMessage_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.GetOrderByIdAsync(It.IsAny<int>()))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.OrderDetailsWithReturn(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Es ist ein unerwarteter Fehler aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Es ist ein unerwarteter Fehler beim abrufen der Bestelldetails der Bestellung mit der Id '1' aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //CancelledOrders
        [Fact]
        public async Task CancelledOrders_ReturnsViewWithOrders_WhenOrdersExist()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var orders = new List<Order>
            {
                new Order { Id = 1, Created = DateTime.UtcNow.AddDays(-1) },
                new Order { Id = 2, Created = DateTime.UtcNow.AddDays(-2) }
            };
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Canceled))
                .ReturnsAsync(orders);

            // Act
            var result = await _controller.CancelledOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Equal(2, model.Count);
            Assert.Equal("Test", result.ViewData["SearchTerm"]);
        }

        [Fact]
        public async Task CancelledOrders_SetsTempDataErrorMessage_WhenValidationExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Field", "Validation error")
            };
            var validationException = new ValidationException(validationFailures);
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Canceled))
                             .ThrowsAsync(validationException);

            // Act
            var result = await _controller.CancelledOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Empty(model);
            Assert.Equal("Validation error", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CancelledOrders_SetsTempDataErrorMessage_WhenOrderServiceExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var exceptionMessage = "Order service error";
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Canceled))
                             .ThrowsAsync(new OrderServiceException(exceptionMessage));

            // Act
            var result = await _controller.CancelledOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Fehler beim Laden der stornierten Bestellungen: " + exceptionMessage, _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CancelledOrders_SetsTempDataErrorMessage_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Canceled))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.CancelledOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Es ist ein unerwarteter Fehler aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler beim abrufen der stornierten Bestellungen aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //DeliveredOrders
         [Fact]
        public async Task DeliveredOrders_ReturnsViewWithOrders_WhenOrdersExist()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var orders = new List<Order>
            {
                new Order { Id = 1, Created = DateTime.UtcNow.AddDays(-1) },
                new Order { Id = 2, Created = DateTime.UtcNow.AddDays(-2) }
            };
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Delivered))
                             .ReturnsAsync(orders);

            // Act
            var result = await _controller.DeliveredOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Equal(2, model.Count);
            Assert.Equal(2, _controller.ViewBag.TotalOrdersCount);
            Assert.Equal("Test", _controller.ViewBag.SearchTerm);
        }

        [Fact]
        public async Task DeliveredOrders_SetsTempDataErrorMessage_WhenValidationExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Field", "Validation error")
            };
            var validationException = new ValidationException(validationFailures);
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Delivered))
                             .ThrowsAsync(validationException);

            // Act
            var result = await _controller.DeliveredOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Empty(model);
            Assert.Equal("Validation error", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeliveredOrders_SetsTempDataErrorMessage_WhenOrderServiceExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var exceptionMessage = "Order service error";
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Delivered))
                             .ThrowsAsync(new OrderServiceException(exceptionMessage));

            // Act
            var result = await _controller.DeliveredOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Fehler beim Laden der gelieferten Bestellungen: " + exceptionMessage, _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeliveredOrders_SetsTempDataErrorMessage_WhenGeneralExceptionIsThrown()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "Test" };
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.SearchOrdersAsync(searchTerm, SharedStatus.Delivered))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.DeliveredOrders(searchTerm) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
            Assert.Equal("Ein unerwarteter Fehler ist aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler beim abrufen der gelieferten Bestellungen aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task DeliveredOrders_LimitsResults_WhenAllIsFalse()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "" };
            var orders = Enumerable.Range(1, 30).Select(i => new Order { Id = i, Created = DateTime.UtcNow.AddDays(-i) }).ToList();
            _orderServiceMock.Setup(s => s.GetAllOrdersByStatusAsync(SharedStatus.Delivered))
                             .ReturnsAsync(orders);

            // Act
            var result = await _controller.DeliveredOrders(searchTerm, false) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Equal(20, model.Count); // Should limit to 20 results
        }

        [Fact]
        public async Task DeliveredOrders_ReturnsAllResults_WhenAllIsTrue()
        {
            // Arrange
            var searchTerm = new SearchTerm { value = "" };
            var orders = Enumerable.Range(1, 30).Select(i => new Order { Id = i, Created = DateTime.UtcNow.AddDays(-i) }).ToList();
            _orderServiceMock.Setup(s => s.GetAllOrdersByStatusAsync(SharedStatus.Delivered))
                             .ReturnsAsync(orders);

            // Act
            var result = await _controller.DeliveredOrders(searchTerm, true) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<Order>>(result.Model);
            var model = result.Model as List<Order>;
            Assert.Equal(30, model.Count); // Should return all results
        }
        
        //CancelOrderEntries
        [Fact]
        public async Task CancelOrderEntries_RedirectsToOrderDetailsWithSuccessMessage_WhenCancellationSucceeds()
        {
            // Arrange
            var orderId = 1;
            var orderCode = "TestCode";
            var cancelledEntries = new Dictionary<int, CancelOrderEntryModel>
            {
                { 1, new CancelOrderEntryModel { IsCancelled = true } }
            };
            _orderServiceMock.Setup(s => s.ProcessOrderEntriesCancellationAsync(orderId, orderCode, cancelledEntries))
                             .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelOrderEntries(orderId, orderCode, cancelledEntries) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("OrderDetails", result.ActionName);
            Assert.Equal(orderId, result.RouteValues["id"]);
            Assert.Equal("Die Stornierung wurde erfolgreich durchgeführt.", _controller.TempData["SuccessMessage"]);
        }

        [Fact]
        public async Task CancelOrderEntries_RedirectsToOrderDetailsWithErrorMessage_WhenCancellationFails()
        {
            // Arrange
            var orderId = 1;
            var orderCode = "TestCode";
            var cancelledEntries = new Dictionary<int, CancelOrderEntryModel>
            {
                { 1, new CancelOrderEntryModel { IsCancelled = true } }
            };
            _orderServiceMock.Setup(s => s.ProcessOrderEntriesCancellationAsync(orderId, orderCode, cancelledEntries))
                             .ReturnsAsync(false);

            // Act
            var result = await _controller.CancelOrderEntries(orderId, orderCode, cancelledEntries) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("OrderDetails", result.ActionName);
            Assert.Equal(orderId, result.RouteValues["id"]);
            Assert.Equal("Es ist ein Fehler bei dem Aufruf der API aufgetreten.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CancelOrderEntries_RedirectsToOrderDetailsWithValidationErrorMessage_WhenValidationExceptionIsThrown()
        {
            // Arrange
            var orderId = 1;
            var orderCode = "TestCode";
            var cancelledEntries = new Dictionary<int, CancelOrderEntryModel>
            {
                { 1, new CancelOrderEntryModel { IsCancelled = true } }
            };
            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("Field", "Validation error")
            };
            var validationException = new ValidationException(validationFailures);
            _orderServiceMock.Setup(s => s.ProcessOrderEntriesCancellationAsync(orderId, orderCode, cancelledEntries))
                             .ThrowsAsync(validationException);

            // Act
            var result = await _controller.CancelOrderEntries(orderId, orderCode, cancelledEntries) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("OrderDetails", result.ActionName);
            Assert.Equal(orderId, result.RouteValues["id"]);
            Assert.Equal("Validation error", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CancelOrderEntries_RedirectsToOrderDetailsWithGeneralErrorMessage_WhenExceptionIsThrown()
        {
            // Arrange
            var orderId = 1;
            var orderCode = "TestCode";
            var cancelledEntries = new Dictionary<int, CancelOrderEntryModel>
            {
                { 1, new CancelOrderEntryModel { IsCancelled = true } }
            };
            var exceptionMessage = "General exception";
            _orderServiceMock.Setup(s => s.ProcessOrderEntriesCancellationAsync(orderId, orderCode, cancelledEntries))
                             .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _controller.CancelOrderEntries(orderId, orderCode, cancelledEntries) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("OrderDetails", result.ActionName);
            Assert.Equal(orderId, result.RouteValues["id"]);
            Assert.Equal("Es ist ein unerwarteter Fehler aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler beim stornieren von Teilen der Bestellung mit der Id '1' aufgetreten.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //ExportToExcel
          [Fact]
        public async Task ExportToExcel_SetsErrorMessageAndRedirectsToIndex_WhenNoOrdersSelected()
        {
            // Arrange
            var selectedOrders = new List<int>();

            // Act
            var result = await _controller.ExportToExcel(selectedOrders) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Bitte markieren Sie mindestens eine Bestellung zum Exportieren.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task ExportToExcel_CreatesExcelFileAndRedirectsToDownloadConfirmation_WhenOrdersAreSelected()
        {
            // Arrange
            var selectedOrders = new List<int> { 1, 2, 3 };
            var orders = new List<Order> { new Order { Id = 1 }, new Order { Id = 2 }, new Order { Id = 3 } };
            var excelContent = new byte[] { 1, 2, 3 };
            var fileId = "file123";

            _orderServiceMock.Setup(s => s.GetOrdersByIds(selectedOrders))
                             .ReturnsAsync(orders);
            _fileServiceMock.Setup(f => f.CreateExcelFileInProgressOrders(orders))
                            .Returns(excelContent);
            _fileServiceMock.Setup(f => f.SaveFileOnServer(excelContent))
                            .Returns(fileId);
            _orderServiceMock.Setup(s => s.UpdateOrderExportedValue(orders, true))
                             .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ExportToExcel(selectedOrders) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("DownloadConfirmation", result.ActionName);
            Assert.Equal(fileId, result.RouteValues["fileId"]);
        }

        [Fact]
        public async Task ExportToExcel_SetsErrorMessageAndRedirectsToIndex_WhenExceptionIsThrown()
        {
            // Arrange
            var selectedOrders = new List<int> { 1, 2, 3 };
            var exceptionMessage = "Export Fehler";
            _orderServiceMock.Setup(s => s.GetOrdersByIds(selectedOrders))
                             .ThrowsAsync(new System.Exception(exceptionMessage));

            // Act
            var result = await _controller.ExportToExcel(selectedOrders) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Es ist ein Fehler beim Exportieren der Daten aufgetreten.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Es ist ein unerwarteter Fehler aufgetreten.")),
                    It.IsAny<System.Exception>(),
                    (Func<It.IsAnyType, System.Exception, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //DownloadConfirmation
        [Fact]
        public void DownloadConfirmation_ReturnsView_WithValidFileId()
        {
            // Arrange
            var fileId = "validFileId";

            // Act
            var result = _controller.DownloadConfirmation(fileId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileId, _controller.ViewBag.FileId);
        }

        [Fact]
        public void DownloadConfirmation_RedirectsToIndex_WithInvalidFileId()
        {
            // Arrange
            string fileId = null;

            // Act
            var result = _controller.DownloadConfirmation(fileId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Ungültige fileId.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Die fileId ist ungültig.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public void DownloadConfirmation_RedirectsToIndex_WithEmptyFileId()
        {
            // Arrange
            var fileId = string.Empty;

            // Act
            var result = _controller.DownloadConfirmation(fileId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Ungültige fileId.", _controller.TempData["ErrorMessage"]);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Die fileId ist ungültig.")),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }
        
        //DownloadFile
        [Fact]
        public async Task DownloadFile_ReturnsFile_WhenFileIdIsValid()
        {
            // Arrange
            var fileId = "validFileId";
            var filePath = "validFilePath";
            _fileServiceMock.Setup(f => f.GetFilePathByFileId(fileId)).Returns(filePath);
            var fileContent = new byte[] { 0x1, 0x2, 0x3 };

            System.IO.Abstractions.TestingHelpers.MockFileSystem fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
            fileSystem.AddFile(filePath, new System.IO.Abstractions.TestingHelpers.MockFileData(fileContent));

            using (var memoryStream = new MemoryStream())
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(fileContent, 0, fileContent.Length);
                }
            }

            // Act
            var result = await _controller.DownloadFile(fileId) as FileResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
            Assert.Equal(Path.GetFileName(filePath), result.FileDownloadName);
        }

        [Fact]
        public async Task DownloadFile_ReturnsNotFound_WhenFileIdIsInvalidOrFileDoesNotExist()
        {
            // Arrange
            var fileId = "invalidFileId";
            _fileServiceMock.Setup(f => f.GetFilePathByFileId(fileId)).Returns<string>(null);

            // Act
            var result = await _controller.DownloadFile(fileId) as NotFoundResult;

            // Assert
            Assert.NotNull(result);
        }
    }
}
