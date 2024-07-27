using System.IO.Abstractions.TestingHelpers;
using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Services;
using System.Runtime.InteropServices;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;

namespace Shared.Tests.Services
{
    public class CsvFileServiceTests
    {
        private readonly Mock<ILogger<CsvFileService>> _loggerMock;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly CsvFileService _csvFileService;
        private readonly IConfiguration _configurationMock;
        private readonly MockFileSystem _fileSystem;


        public CsvFileServiceTests()
        {
            _loggerMock = new Mock<ILogger<CsvFileService>>();
            _orderServiceMock = new Mock<IOrderService>();
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", "ValidPath"),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", "ValidPath"),
                new KeyValuePair<string, string?>("CustomerSettings:CustomerNumber", "123")
            };

            _configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
            _fileSystem = new MockFileSystem();

            _csvFileService = new CsvFileService(_configurationMock, _loggerMock.Object, _orderServiceMock.Object, _fileSystem);
        }
        
        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsNotSupportedException_WhenOSIsNotSupported()
        {
            // Arrange
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object, _fileSystem);

            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Act & Assert
                Assert.Throws<NotSupportedException>(() => csvFileService.GetConsignmentsFromCsvFiles());
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

        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsDirectoryNotFoundException_WhenFolderPathIsInvalid()
        {
            // Arrange
            _fileSystem.AddDirectory(@"C:\CsvFiles");
            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => _csvFileService.GetConsignmentsFromCsvFiles());
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
        public void GetConsignmentsFromCsvFiles_ThrowsDirectoryNotFoundException_WhenFolderPathDoesNotExist()
        {
            // Arrange
            string folderPath = "InvalidPath";
         
            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => _csvFileService.GetConsignmentsFromCsvFiles());
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
        public void GetConsignmentsFromCsvFiles_ParsesCsvFilesSuccessfully()
        {
            // Arrange
            string folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folderPath);
            
            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Create mock CSV content
            string csvContent =
                "paket;kdnr;datum_druck;lieferschein;nve_nr;kontrakt_nr_kunde;name1;name2;strasse;nation;plz;ort;vers_text;verpackungs_nr;retoure_nr;farbe_id;artikelnummer;menge\n" +
                "1;123;01.01.2023;456;789;999;John Doe;Jane Doe;Musterstrasse;DE;12345;Musterstadt;;111;;;ART123;10\n";
            string filePath = Path.Combine(folderPath, "test.csv");
            File.WriteAllText(filePath, csvContent);

            // Act
            var result = _csvFileService.GetConsignmentsFromCsvFiles();

            // Assert
            Assert.Single(result);
            Assert.Equal("1", result[0].paket);
            Assert.Equal("123", result[0].kdnr);
            Assert.Equal("01.01.2023", result[0].datum_druck);
            Assert.Equal("456", result[0].lieferschein);
            Assert.Equal("789", result[0].nve_nr);
            Assert.Equal("999", result[0].kontrakt_nr_kunde);
            Assert.Equal("John Doe", result[0].name1);
            Assert.Equal("Jane Doe", result[0].name2);
            Assert.Equal("Musterstrasse", result[0].strasse);
            Assert.Equal("DE", result[0].nation);
            Assert.Equal("12345", result[0].plz);
            Assert.Equal("Musterstadt", result[0].ort);
            Assert.Equal("", result[0].vers_text);
            Assert.Equal("111", result[0].verpackungs_nr);
            Assert.Equal("", result[0].retoure_nr);
            Assert.Equal("", result[0].farbe_id);
            Assert.Equal("ART123", result[0].artikelnummer);
            Assert.Equal("10", result[0].menge);

            // Cleanup
            File.Delete(filePath);
            Directory.Delete(folderPath);
        }

        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsHeaderValidationException_OnCsvHeaderValidationError()
        {
            // Arrange
            string folderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folderPath);
            
            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Create mock invalid CSV content with missing headers
            string csvContent =
                "strasse;nation;plz;ort;vers_text;verpackungs_nr;retoure_nr;farbe_id;artikelnummer;menge\n" +
                "1;John Doe;Jane Doe;Musterstrasse;DE;12345;Musterstadt;;111;;;ART123;10";
            string filePath = Path.Combine(folderPath, "test.csv");
            File.WriteAllText(filePath, csvContent);

            // Act & Assert
            Assert.Throws<HeaderValidationException>(() => _csvFileService.GetConsignmentsFromCsvFiles());
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
            // Cleanup
            File.Delete(filePath);
            Directory.Delete(folderPath);
        }
        
        //ParseConsignmentsFromCsvToConsignments
        [Fact]
        public async Task ParseConsignmentsFromCsvToConsignments_ReturnsEmptyList_WhenNoCustomerNumberMatch()
        {
            // Arrange
            var csvConsignments = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv { kdnr = "999", nve_nr = "tracking1", artikelnummer = "ART123", menge = "10" }
            };

            // Act
            var result = await _csvFileService.ParseConsignmentsFromCsvToConsignments(csvConsignments);

            // Assert
            Assert.Empty(result);
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
        public async Task ParseConsignmentsFromCsvToConsignments_GroupsByTrackingNumber()
        {
            // Arrange
            var csvConsignments = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "123", nve_nr = "tracking1", artikelnummer = "ART", menge = "10", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001"},
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "124", nve_nr = "tracking1", artikelnummer = "ART", menge = "5", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001" }
            };

            var order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry { VendorProductCode = "ART123", Id = 1, EntryNumber = 1 },
                    new OrderEntry { VendorProductCode = "ART124", Id = 2, EntryNumber = 2 }
                }
            };

            _orderServiceMock.Setup(service => service.GetOrderByOrderCodeAsync("ORDER1")).ReturnsAsync(order);

            // Act
            var result = await _csvFileService.ParseConsignmentsFromCsvToConsignments(csvConsignments);

            // Assert
            Assert.Single(result);
            Assert.Equal("tracking1", result[0].TrackingId);
            Assert.Equal(2, result[0].ConsignmentEntries.Count);
        }

        [Fact]
        public async Task ParseConsignmentsFromCsvToConsignments_SkipsEntriesWithoutMatchingOrderEntry()
        {
            // Arrange
            var csvConsignments = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "123", nve_nr = "tracking1", artikelnummer = "ART", menge = "10", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001"},
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "124", nve_nr = "tracking1", artikelnummer = "ART", menge = "5", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001" }
            };

            var order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry { VendorProductCode = "ART123", Id = 1, EntryNumber = 1 }
                }
            };

            _orderServiceMock.Setup(service => service.GetOrderByOrderCodeAsync("ORDER1")).ReturnsAsync(order);

            // Act
            var result = await _csvFileService.ParseConsignmentsFromCsvToConsignments(csvConsignments);

            // Assert
            Assert.Single(result[0].ConsignmentEntries);
            _loggerMock.Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
        }

        [Fact]
        public async Task ParseConsignmentsFromCsvToConsignments_SetsTrackingLinkForCarrier()
        {
            // Arrange
            var csvConsignments = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "123", nve_nr = "tracking1", artikelnummer = "ART", menge = "10", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001"}
            };

            var order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry { VendorProductCode = "ART123", Id = 1, EntryNumber = 1 }
                }
            };

            _orderServiceMock.Setup(service => service.GetOrderByOrderCodeAsync("ORDER1")).ReturnsAsync(order);

            // Act
            var result = await _csvFileService.ParseConsignmentsFromCsvToConsignments(csvConsignments);

            // Assert
            Assert.Single(result);
            Assert.Equal("https://tracking.dpd.de/parcelstatus?query=tracking1", result[0].TrackingLink);
        }

        [Fact]
        public async Task ParseConsignmentsFromCsvToConsignments_SetsShippingAddress()
        {
            // Arrange
            var csvConsignments = new List<ConsignmentFromCsv>
            {
                new ConsignmentFromCsv { kdnr = "123", paket = "DPD", farbe_id = "123", nve_nr = "tracking1", artikelnummer = "ART", menge = "10", kontrakt_nr_kunde = "ORDER1", datum_druck = "01.01.2001"}
            };

            var order = new Order
            {
                Entries = new List<OrderEntry>
                {
                    new OrderEntry
                    {
                        VendorProductCode = "ART123",
                        Id = 1,
                        EntryNumber = 1,
                        DeliveryAddress = new DeliveryAddress
                        {
                            Type = "Type",
                            SalutationCode = "Herr",
                            FirstName = "John",
                            LastName = "Doe",
                            StreetName = "Musterstrasse",
                            StreetNumber = "1",
                            PostalCode = "12345",
                            Town = "Musterstadt",
                            CountryIsoCode = "DE"
                        }
                    }
                }
            };

            _orderServiceMock.Setup(service => service.GetOrderByOrderCodeAsync("ORDER1")).ReturnsAsync(order);

            // Act
            var result = await _csvFileService.ParseConsignmentsFromCsvToConsignments(csvConsignments);

            // Assert
            Assert.Single(result);
            var shippingAddress = result[0].ShippingAddress;
            Assert.NotNull(shippingAddress);
            Assert.Equal("Type", shippingAddress.Type);
            Assert.Equal("Herr", shippingAddress.SalutationCode);
            Assert.Equal("John", shippingAddress.FirstName);
            Assert.Equal("Doe", shippingAddress.LastName);
            Assert.Equal("Musterstrasse", shippingAddress.StreetName);
            Assert.Equal("1", shippingAddress.StreetNumber);
            Assert.Equal("12345", shippingAddress.PostalCode);
            Assert.Equal("Musterstadt", shippingAddress.Town);
            Assert.Equal("DE", shippingAddress.CountryIsoCode);
        }
        
        //MoveCsvFilesToArchiv
        [Fact]
        public void MoveCsvFilesToArchiv_MovesFilesSuccessfully()
        {
            // Arrange
            string sourceFolderPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles");
            string archiveFolderPath = _fileSystem.Path.Combine(sourceFolderPath, "Archiv");
        
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles")),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles"))
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object, _fileSystem);
            
            _fileSystem.AddDirectory(sourceFolderPath);
            _fileSystem.AddFile(_fileSystem.Path.Combine(sourceFolderPath, "file1.csv"), new MockFileData("content1"));
            _fileSystem.AddFile(_fileSystem.Path.Combine(sourceFolderPath, "file2.csv"), new MockFileData("content2"));
        
            // Act
            csvFileService.MoveCsvFilesToArchiv();
        
            // Assert
            Assert.False(_fileSystem.File.Exists(_fileSystem.Path.Combine(sourceFolderPath, "file1.csv")));
            Assert.False(_fileSystem.File.Exists(_fileSystem.Path.Combine(sourceFolderPath, "file2.csv")));
            Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(archiveFolderPath, "file1.csv")));
            Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(archiveFolderPath, "file2.csv")));
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
         public void MoveCsvFilesToArchiv_CreatesArchiveFolderIfNotExists()
         {
             // Arrange
             string sourceFolderPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles");
             string archiveFolderPath = _fileSystem.Path.Combine(sourceFolderPath, "Archiv");
        
             _fileSystem.AddDirectory(sourceFolderPath);
             _fileSystem.AddFile(_fileSystem.Path.Combine(sourceFolderPath, "file1.csv"), new MockFileData("content1"));
        
             var configurationData = new List<KeyValuePair<string, string?>>
             {
                 new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles")),
                 new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles"))
             };
             var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
             var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object, _fileSystem);

             
             // Act
             csvFileService.MoveCsvFilesToArchiv();
        
             // Assert
             Assert.True(_fileSystem.Directory.Exists(archiveFolderPath));
             Assert.False(_fileSystem.File.Exists(_fileSystem.Path.Combine(sourceFolderPath, "file1.csv")));
             Assert.True(_fileSystem.File.Exists(_fileSystem.Path.Combine(archiveFolderPath, "file1.csv")));
         }
        
         [Fact]
         public void MoveCsvFilesToArchiv_LogsErrorOnException()
         {
             // Arrange
             string sourceFolderPath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles");
             _fileSystem.AddDirectory(sourceFolderPath);
             _fileSystem.AddFile(_fileSystem.Path.Combine(sourceFolderPath, "file1.csv"), new MockFileData("content1"));
        
             // ReadOnly damit Fehler erzeugt wird
             var filePath = _fileSystem.Path.Combine(sourceFolderPath, "file1.csv");
             _fileSystem.File.SetAttributes(filePath, System.IO.FileAttributes.ReadOnly);
        
             var configurationData = new List<KeyValuePair<string, string?>>
             {
                 new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles")),
                 new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), "CsvFiles"))
             };
             var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();
             var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object, _fileSystem);

             // Act
             csvFileService.MoveCsvFilesToArchiv();
        
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
}