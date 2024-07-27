using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Services;
using System.Runtime.InteropServices;
using Shared.Contracts;

namespace Shared.Tests.Services
{
    public class CsvFileServiceTests
    {
        private readonly Mock<ILogger<CsvFileService>> _loggerMock;
        private readonly Mock<IOrderService> _orderServiceMock;
        private readonly CsvFileService _csvFileService;

        public CsvFileServiceTests()
        {
            _loggerMock = new Mock<ILogger<CsvFileService>>();
            _orderServiceMock = new Mock<IOrderService>();
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();


            _csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);
        }
        
        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsNotSupportedException_WhenFolderPathIsInvalid()
        {
            // Arrange
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();

            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);

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

        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsDirectoryNotFoundException_WhenFolderPathIsInvalid()
        {
            // Arrange
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();


            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);

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

        [Fact]
        public void GetConsignmentsFromCsvFiles_ThrowsDirectoryNotFoundException_WhenFolderPathDoesNotExist()
        {
            // Arrange
            string folderPath = "InvalidPath";
            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();


            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);

            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => csvFileService.GetConsignmentsFromCsvFiles());
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

            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();


            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);

            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Create mock CSV content
            string csvContent =
                "paket;kdnr;datum_druck;lieferschein;nve_nr;kontrakt_nr_kunde;name1;name2;strasse;nation;plz;ort;vers_text;verpackungs_nr;retoure_nr;farbe_id;artikelnummer;menge\n" +
                "1;123;01.01.2023;456;789;999;John Doe;Jane Doe;Musterstrasse;DE;12345;Musterstadt;;111;;;ART123;10\n";
            string filePath = Path.Combine(folderPath, "test.csv");
            File.WriteAllText(filePath, csvContent);

            // Act
            var result = csvFileService.GetConsignmentsFromCsvFiles();

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

            var configurationData = new List<KeyValuePair<string, string?>>
            {
                new KeyValuePair<string, string?>("CsvConsignmentPath:Windows", string.Empty),
                new KeyValuePair<string, string?>("CsvConsignmentPath:MacOS", string.Empty)
            };
            var configurationMock = new ConfigurationBuilder().AddInMemoryCollection(configurationData).Build();


            var csvFileService = new CsvFileService(configurationMock, _loggerMock.Object, _orderServiceMock.Object);

            // Mock OS Platform
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                configurationMock["CsvConsignmentPath:Windows"] = folderPath;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                configurationMock["CsvConsignmentPath:MacOS"] = folderPath;
            }

            // Create mock invalid CSV content with missing headers
            string csvContent =
                "strasse;nation;plz;ort;vers_text;verpackungs_nr;retoure_nr;farbe_id;artikelnummer;menge\n" +
                "1;John Doe;Jane Doe;Musterstrasse;DE;12345;Musterstadt;;111;;;ART123;10";
            string filePath = Path.Combine(folderPath, "test.csv");
            File.WriteAllText(filePath, csvContent);

            // Act & Assert
            Assert.Throws<HeaderValidationException>(() => csvFileService.GetConsignmentsFromCsvFiles());
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
    }
}