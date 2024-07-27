using System.Collections.Concurrent;
using System.Text;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Shared.Services;
using Xunit;

namespace Shared.Tests.Services
{
    public class FileServiceTests
    {
        private readonly Mock<ILogger<FileService>> _loggerMock;
        private readonly Mock<IFileWrapper> _fileWrapperMock;
        private readonly IOptions<CustomerSettings> _customerSettings;
        private readonly Mock<IFontResolver> _fontResolverMock;
        private readonly Mock<IExcelWorkbook> _excelWorkbookMock;
        private readonly Mock<IGuidGenerator> _guidGeneratorMock;
        private readonly Mock<IFileMapping> _fileMappingMock;
        private readonly Mock<IImageLoader> _imageLoaderMock;
        private readonly FileService _fileService;
        private readonly IOptions<FileSettings> _fileSettings;
        private readonly List<Order> _testOrders;
        private readonly Return _testReturn;

        private static readonly ConcurrentDictionary<string, string> FileMappings =
            new ConcurrentDictionary<string, string>();

        public FileServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileService>>();
            _fileWrapperMock = new Mock<IFileWrapper>();
            _fontResolverMock = new Mock<IFontResolver>();
            _excelWorkbookMock = new Mock<IExcelWorkbook>();
            _guidGeneratorMock = new Mock<IGuidGenerator>();
            _fileMappingMock = new Mock<IFileMapping>();
            _imageLoaderMock = new Mock<IImageLoader>();

            var customerSettingsValue = new CustomerSettings
            {
                CustomerNumber = "12345",
                CustomerIln = "67890"
            };
            _customerSettings = Options.Create(customerSettingsValue);

            var fileSettingsValue = new FileSettings
            {
                UploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads")
            };
            _fileSettings = Options.Create(fileSettingsValue);

            _fileService = new FileService(_loggerMock.Object, _customerSettings, _fontResolverMock.Object,
                _excelWorkbookMock.Object, _fileWrapperMock.Object, _guidGeneratorMock.Object,
                _fileMappingMock.Object, _imageLoaderMock.Object, _fileSettings);

            _testOrders = new List<Order>
            {
                new Order
                {
                    Code = "Order1",
                    Phone = "1234567890",
                    Status = SharedStatus.InProgress,
                    Entries = new List<OrderEntry>
                    {
                        new OrderEntry
                        {
                            Quantity = 10,
                            CanceledOrReturnedQuantity = 0,
                            VendorProductCode = "TEST",
                            ProductName = "TEST",
                            DeliveryAddress = new DeliveryAddress
                            {
                                FirstName = "John",
                                LastName = "Doe",
                                StreetName = "Main",
                                StreetNumber = "1",
                                PostNumber = "12345",
                                PackstationNumber = "67890",
                                PostOfficeNumber = "111",
                                CountryIsoCode = "DE",
                                PostalCode = "10115",
                                Town = "Berlin",
                                Type = SharedStatus.Default
                            }
                        }
                    }
                }
            };

            _testReturn = new Return
            {
                OrderCode = "Order123",
                InitiationDate = DateTime.Now,
                CustomerInfo = new CustomerInfo
                {
                    Address = new Address
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        StreetName = "Main",
                        StreetNumber = "100",
                        PostalCode = "12345",
                        Town = "Townsville",
                        CountryIsoCode = "DE"
                    }
                },
                ReturnEntries = new List<ReturnEntry>
                {
                    new ReturnEntry
                    {
                        OrderEntryNumber = 1,
                        Reason = "Damaged",
                        ReturnConsignments = new List<ReturnConsignment>
                        {
                            new ReturnConsignment
                            {
                                Quantity = 1,
                                Packages = new List<ReturnPackage>
                                {
                                    new ReturnPackage { TrackingId = "123ABC" }
                                }
                            }
                        }
                    }
                },
                Order = new Order { Entries = new List<OrderEntry>() }
            };
            
            SetupFontResolver();

        }
        
        private void SetupFontResolver()
        {
            _fontResolverMock.Setup(f => f.ResolveTypeface("Arial", false, false))
                .Returns(new FontResolverInfo("Arial"));
            _fontResolverMock.Setup(f => f.GetFont("Arial"))
                .Returns(File.ReadAllBytes("Fonts/arial.ttf"));

            _fontResolverMock.Setup(f => f.ResolveTypeface("Arial_bold", true, false))
                .Returns(new FontResolverInfo("Arial_bold"));
            _fontResolverMock.Setup(f => f.GetFont("Arial_bold"))
                .Returns(File.ReadAllBytes("Fonts/arialbd.ttf"));

            GlobalFontSettings.FontResolver = _fontResolverMock.Object;
        }
        
        private byte[] CreateSampleExcelFile()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Orders");

                // Kopfzeilen
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "iln";
                worksheet.Cell(1, 3).Value = "order_number";
                worksheet.Cell(1, 4).Value = "address_1";
                worksheet.Cell(1, 5).Value = "address_2";
                worksheet.Cell(1, 6).Value = "street";
                worksheet.Cell(1, 7).Value = "country_id";
                worksheet.Cell(1, 8).Value = "postal_code";
                worksheet.Cell(1, 9).Value = "city";
                worksheet.Cell(1, 10).Value = "tel";
                worksheet.Cell(1, 11).Value = "qty";
                worksheet.Cell(1, 12).Value = "art_no";
                worksheet.Cell(1, 13).Value = "article_description";

                worksheet.Cell(2, 1).Value = "12345"; // CustomerNumber
                worksheet.Cell(2, 2).Value = "67890"; // CustomerIln
                worksheet.Cell(2, 3).Value = "Order1"; // Order code
                worksheet.Cell(2, 4).Value = "John Doe"; // First Name + Last Name
                worksheet.Cell(2, 5).Value = ""; // Weitere Adressinformationen oder leer
                worksheet.Cell(2, 6).Value = "Main 1"; // Straße und Nummer
                worksheet.Cell(2, 7).Value = "DE"; // CountryIsoCode
                worksheet.Cell(2, 8).Value = "10115"; // PostalCode
                worksheet.Cell(2, 9).Value = "Berlin"; // Town
                worksheet.Cell(2, 10).Value = "1234567890"; // Phone
                worksheet.Cell(2, 11).Value = 10; // Quantity
                worksheet.Cell(2, 12).Value = "TEST"; // VendorProductCode
                worksheet.Cell(2, 13).Value = "TEST"; // ProductName

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return stream.ToArray();
                }
            }
        }

        private byte[] CreateHeaderOnlyExcelFile()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Orders");

                // Definiere die Kopfzeilen
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "iln";
                worksheet.Cell(1, 3).Value = "order_number";
                worksheet.Cell(1, 4).Value = "address_1";
                worksheet.Cell(1, 5).Value = "address_2";
                worksheet.Cell(1, 6).Value = "street";
                worksheet.Cell(1, 7).Value = "country_id";
                worksheet.Cell(1, 8).Value = "postal_code";
                worksheet.Cell(1, 9).Value = "city";
                worksheet.Cell(1, 10).Value = "tel";
                worksheet.Cell(1, 11).Value = "qty";
                worksheet.Cell(1, 12).Value = "art_no";
                worksheet.Cell(1, 13).Value = "article_description";

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return stream.ToArray();
                }
            }
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_CreatesValidFile()
        {
            // Setup
            byte[] sampleExcelContent = CreateSampleExcelFile();
            _excelWorkbookMock.Setup(x => x.AddWorksheet("Orders")).Returns(new XLWorkbook().AddWorksheet("Orders"));
            _excelWorkbookMock.Setup(x => x.SaveAs(It.IsAny<Stream>())).Callback<Stream>(stream =>
            {
                stream.Write(sampleExcelContent, 0, sampleExcelContent.Length);
                stream.Position = 0;
            });

            // Act
            var result = _fileService.CreateExcelFileInProgressOrders(_testOrders);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);

            _excelWorkbookMock.Verify(x => x.AddWorksheet("Orders"),
                Times.Once); // Bestätigt, dass AddWorksheet aufgerufen wurde
            _excelWorkbookMock.Verify(x => x.SaveAs(It.IsAny<Stream>()),
                Times.Once); // Bestätigt, dass SaveAs aufgerufen wurde

            using (var stream = new MemoryStream(result))
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                Assert.Equal("ID", worksheet.Cell(1, 1).Value);
                Assert.Equal("12345", worksheet.Cell(2, 1).Value); // CustomerNumber
                Assert.Equal("67890", worksheet.Cell(2, 2).Value); // CustomerIln
                Assert.Equal("Order1", worksheet.Cell(2, 3).Value); // Order code
                Assert.Equal("John Doe", worksheet.Cell(2, 4).Value); // Delivery address
                Assert.Equal("Main 1", worksheet.Cell(2, 6).Value); // Street and number
                Assert.Equal("DE", worksheet.Cell(2, 7).Value); // CountryIsoCode
                Assert.Equal("10115", worksheet.Cell(2, 8).Value); // PostalCode
                Assert.Equal("Berlin", worksheet.Cell(2, 9).Value); // Town
                Assert.Equal("1234567890", worksheet.Cell(2, 10).Value); // Phone
                Assert.Equal(10, worksheet.Cell(2, 11).Value); // Quantity
                Assert.Equal("TEST", worksheet.Cell(2, 12).Value); // VendorProductCode
                Assert.Equal("TEST", worksheet.Cell(2, 13).Value); // ProductName
            }
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_EmptyOrders()
        {
            // Arrange
            var emptyOrders = new List<Order>();
            byte[] headerOnlyContent = CreateHeaderOnlyExcelFile();
            _excelWorkbookMock.Setup(x => x.AddWorksheet("Orders")).Returns(new XLWorkbook().AddWorksheet("Orders"));
            _excelWorkbookMock.Setup(x => x.SaveAs(It.IsAny<Stream>())).Callback<Stream>(stream =>
            {
                stream.Write(headerOnlyContent, 0, headerOnlyContent.Length);
                stream.Position = 0;
            });

            // Act
            var result = _fileService.CreateExcelFileInProgressOrders(emptyOrders);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0,
                "Die Datei sollte nicht leer sein, selbst wenn keine Bestellungen vorhanden sind.");
            using (var stream = new MemoryStream(result))
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                Assert.Equal("ID", worksheet.Cell(1, 1).Value);
                Assert.True(worksheet.RowsUsed().Count() == 1, "Es sollte nur eine Zeile für Kopfzeilen vorhanden sein.");
            }
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_AllItemsCancelled()
        {
            // Arrange
            var ordersWithAllCancelled = new List<Order>
            {
                new Order
                {
                    Code = "Order1",
                    Phone = "1234567890",
                    Entries = new List<OrderEntry>
                    {
                        new OrderEntry
                        {
                            DeliveryAddress = new DeliveryAddress(),
                            Quantity = 10,
                            CanceledOrReturnedQuantity = 10
                        }
                    }
                }
            };

            byte[] headerOnlyContent = CreateHeaderOnlyExcelFile();
            _excelWorkbookMock.Setup(x => x.AddWorksheet("Orders")).Returns(new XLWorkbook().AddWorksheet("Orders"));
            _excelWorkbookMock.Setup(x => x.SaveAs(It.IsAny<Stream>())).Callback<Stream>(stream =>
            {
                stream.Write(headerOnlyContent, 0, headerOnlyContent.Length);
                stream.Position = 0;
            });

            // Act
            var result = _fileService.CreateExcelFileInProgressOrders(ordersWithAllCancelled);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0,
                "Die Datei sollte nicht leer sein, selbst wenn alle Bestellungen storniert wurden.");

            using (var stream = new MemoryStream(result))
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                Assert.Equal("ID", worksheet.Cell(1, 1).Value);
                Assert.True(worksheet.RowsUsed().Count() == 1,
                    "Es sollte nur eine Zeile für Kopfzeilen vorhanden sein, da alle Artikel storniert wurden.");
            }
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_LogsError_WhenOrderEntryIsNull()
        {
            // Arrange
            var ordersWithNullEntry = new List<Order>
            {
                new Order
                {
                    Code = "Order1",
                    Phone = "1234567890",
                    Entries = null
                }
            };

            _excelWorkbookMock.Setup(x => x.AddWorksheet("Orders")).Returns(new XLWorkbook().AddWorksheet("Orders"));

            // Act & Assert
            _fileService.CreateExcelFileInProgressOrders(ordersWithNullEntry);

            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_LogsError_WhenDeliveryAddressIsNull()
        {
            // Arrange
            var ordersWithNullEntry = new List<Order>
            {
                new Order
                {
                    Code = "Order1",
                    Phone = "1234567890",
                    Entries = new List<OrderEntry>
                    {
                        new OrderEntry
                        {
                            Id = 1,
                            Quantity = 2,
                            DeliveryAddress = null
                        }
                    }
                }
            };

            _excelWorkbookMock.Setup(x => x.AddWorksheet("Orders")).Returns(new XLWorkbook().AddWorksheet("Orders"));

            // Act & Assert
            _fileService.CreateExcelFileInProgressOrders(ordersWithNullEntry);

            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Exactly(2));
        }

        [Fact]
        public void CreateExcelFileInProgressOrders_HandlesUnexpectedException()
        {
            // Arrange
            _excelWorkbookMock.Setup(wb => wb.AddWorksheet(It.IsAny<string>()))
                .Throws(new Exception("Unerwarteter Fehler"));

            // Act
            var result = _fileService.CreateExcelFileInProgressOrders(_testOrders);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
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
        public void SaveFileOnServer_SavesFileSuccessfully_ReturnsFileId()
        {
            // Arrange
            var expectedGuid = Guid.NewGuid().ToString();
            _guidGeneratorMock.Setup(g => g.NewGuid()).Returns(new Guid(expectedGuid));

            var content = Encoding.UTF8.GetBytes("Test content");
            var uploadsFolder = _fileSettings.Value.UploadFolder;
            var fileName = $"Aldi_Export_{expectedGuid}.xls";
            var filePath = Path.Combine(uploadsFolder, fileName);

            _fileWrapperMock.Setup(fw => fw.DirectoryExists(uploadsFolder)).Returns(true);
            _fileWrapperMock.Setup(fw => fw.WriteAllBytes(filePath, content)).Verifiable();

            // Act
            var fileId = _fileService.SaveFileOnServer(content);

            // Assert
            Assert.Equal(expectedGuid, fileId);
            _fileWrapperMock.Verify(fw => fw.WriteAllBytes(filePath, content), Times.Once);
        }

        [Fact]
        public void SaveFileOnServer_WhenDirectoryIsReadOnly_ThrowsFileSaveException()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("Test content");

            _fileWrapperMock.Setup(fw => fw.DirectoryExists(It.IsAny<string>())).Returns(true);
            _fileWrapperMock.Setup(fw => fw.CreateDirectory(It.IsAny<string>()));
            _fileWrapperMock.Setup(fw => fw.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Throws(new IOException("Access to the path is denied."));

            // Act & Assert
            Assert.Throws<FileSaveException>(() => _fileService.SaveFileOnServer(content));

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
        public void SaveFileOnServer_WhenAccessIsDenied_ThrowsFileSaveException()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("Test content");
            var accessDeniedDirectory = "/path/to/access-denied/directory";
            var fileName = $"Aldi_Export_{Guid.NewGuid().ToString()}.xls";
            var filePath = Path.Combine(accessDeniedDirectory, fileName);

            _fileWrapperMock.Setup(fw => fw.DirectoryExists(accessDeniedDirectory)).Returns(true);
            _fileWrapperMock.Setup(fw => fw.CreateDirectory(It.IsAny<string>()));
            _fileWrapperMock.Setup(fw => fw.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Throws(new UnauthorizedAccessException("Access to the path is denied."));

            // Act & Assert
            Assert.Throws<FileSaveException>(() => _fileService.SaveFileOnServer(content));

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
        public void SaveFileOnServer_ThrowsFileSaveException_WhenUnexpectedExceptionOccurs()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("Test content");
            var accessDeniedDirectory = "/path/to/access-denied/directory";
            var fileName = $"Aldi_Export_{Guid.NewGuid().ToString()}.xls";
            var filePath = Path.Combine(accessDeniedDirectory, fileName);

            _fileWrapperMock.Setup(fw => fw.DirectoryExists(accessDeniedDirectory)).Returns(true);
            _fileWrapperMock.Setup(fw => fw.CreateDirectory(It.IsAny<string>()));
            _fileWrapperMock.Setup(fw => fw.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()))
                .Throws(new Exception("Unexpected exception."));

            // Act & Assert
            Assert.Throws<FileSaveException>(() => _fileService.SaveFileOnServer(content));

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
        public void GetFilePathByFileId_ReturnsCorrectFilePath_WhenFileIdExists()
        {
            // Arrange
            var fileId = "validFileId";
            var expectedFilePath = "/path/to/file";
            _fileMappingMock.Setup(fm => fm.GetFilePath(fileId)).Returns(expectedFilePath);

            // Act
            var result = _fileService.GetFilePathByFileId(fileId);

            // Assert
            Assert.Equal(expectedFilePath, result);
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void GetFilePathByFileId_ReturnsEmpty_WhenFileIdDoesNotExist()
        {
            // Arrange
            var invalidFileId = "invalidFileId";
            _fileMappingMock.Setup(fm => fm.GetFilePath(invalidFileId)).Returns((string?)null);

            // Act
            var result = _fileService.GetFilePathByFileId(invalidFileId);

            // Assert
            Assert.Empty(result);
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
        public void GetFilePathByFileId_ReturnsEmpty_WhenFilePathIsNull()
        {
            // Arrange
            var fileId = "fileIdWithNullPath";
            _fileMappingMock.Setup(fm => fm.GetFilePath(fileId)).Returns((string?)null);

            // Act
            var result = _fileService.GetFilePathByFileId(fileId);

            // Assert
            Assert.Empty(result);
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
        public void GetFilePathByFileId_ReturnsEmpty_WhenFilePathIsEmpty()
        {
            // Arrange
            var fileId = "fileIdWithEmptyPath";
            _fileMappingMock.Setup(fm => fm.GetFilePath(fileId)).Returns(string.Empty);

            // Act
            var result = _fileService.GetFilePathByFileId(fileId);

            // Assert
            Assert.Empty(result);
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
        public void GeneratePdf_ThrowsReturnIsNullException_WhenReturnIsNull()
        {
            // Act & Assert
            Assert.Throws<ReturnIsNullException>(() => _fileService.GeneratePdf(null));
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
        public void GeneratePdf_ReturnsNonEmptyStream_WhenReturnIsValid()
        {
            string imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "aldi.png");
            XImage testImage = XImage.FromFile(imagePath);

            _imageLoaderMock.Setup(x => x.LoadImage(It.IsAny<string>())).Returns(testImage);

            var result = _fileService.GeneratePdf(_testReturn);

            Assert.NotNull(result);
            Assert.True(result.Length > 0, "Generated PDF stream should not be empty.");
        }
        
        [Fact]
        public void GeneratePdf_ThrowsPdfGenerationException_OnPdfCreationFailure()
        {
            // Arrange
            _imageLoaderMock.Setup(x => x.LoadImage(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Failed to load image."));

            // Act & Assert
            Assert.Throws<PdfGenerationException>(() => _fileService.GeneratePdf(_testReturn));
        }

        [Fact]
        public void GeneratePdf_ThrowsPdfGenerationException_OnFontLoadingFailure()
        {
            // Arrange
            var returnObj = new Return { /* initialize with valid data */ };
            _fontResolverMock.Setup(f => f.GetFont(It.IsAny<string>()))
                .Throws(new FileNotFoundException("Font file not found."));

            // Act & Assert
            Assert.Throws<PdfGenerationException>(() => _fileService.GeneratePdf(returnObj));
        }
    }
}
