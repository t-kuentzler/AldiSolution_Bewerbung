using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Mappings;
using Shared.Models;

namespace Shared.Services;

public class FileService : IFileService
    {
        private static readonly ConcurrentDictionary<string, string> FileMappings =
            new ConcurrentDictionary<string, string>();

        private readonly ILogger<FileService> _logger;
        private readonly CustomerSettings _customerSettings;
        private readonly IFontResolver _fontResolver;
        private readonly IExcelWorkbook _excelWorkbook;
        private readonly IFileWrapper _fileWrapper;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IFileMapping _fileMapping;
        private readonly IImageLoader _imageLoader;
        private readonly FileSettings _fileSettings;

        public FileService(ILogger<FileService> logger, IOptions<CustomerSettings> customerSettings,
            IFontResolver fontResolver, IExcelWorkbook excelWorkbook, IFileWrapper fileWrapper,
            IGuidGenerator guidGenerator, IFileMapping fileMapping, IImageLoader imageLoader, IOptions<FileSettings> fileSettings)
        {
            _logger = logger;
            _customerSettings = customerSettings.Value;
            _fontResolver = fontResolver;
            _excelWorkbook = excelWorkbook;
            _fileWrapper = fileWrapper;
            _guidGenerator = guidGenerator;
            _fileMapping = fileMapping;
            _imageLoader = imageLoader;
            _fileSettings = fileSettings.Value;
        }

        public byte[] CreateExcelFileInProgressOrders(List<Order> orders)
        {
            try
            {
                _logger.LogInformation(
                    $"Es wird versucht, eine Excel Datei mit allen Bestellungen des Status '{SharedStatus.InProgress}' zu erstellen.");

                var worksheet = _excelWorkbook.AddWorksheet("Orders");
                var currentRow = 1;

                // Kopfzeilen hinzufügen
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "iln";
                worksheet.Cell(currentRow, 3).Value = "order_number";
                worksheet.Cell(currentRow, 4).Value = "address_1";
                worksheet.Cell(currentRow, 5).Value = "address_2";
                worksheet.Cell(currentRow, 6).Value = "street";
                worksheet.Cell(currentRow, 7).Value = "country_id";
                worksheet.Cell(currentRow, 8).Value = "postal_code";
                worksheet.Cell(currentRow, 9).Value = "city";
                worksheet.Cell(currentRow, 10).Value = "tel";
                worksheet.Cell(currentRow, 11).Value = "qty";
                worksheet.Cell(currentRow, 12).Value = "art_no";
                worksheet.Cell(currentRow, 13).Value = "article_description";

                // Daten für jede Bestellung hinzufügen
                foreach (var order in orders)
                {
                    if (order.Entries == null)
                    {
                        _logger.LogError("Die Datei konnte nicht erzeugt werden, da OrderEntry null ist.");
                        throw new OrderEntryIsNullException();
                    }
                    foreach (var entry in order.Entries)
                    {
                        if (entry.DeliveryAddress == null)
                        {
                            _logger.LogError("Die Datei konnte nicht erzeugt werden, da DeliveryAddress null ist.");
                            throw new DeliveryAddressIsNullException();
                        }

                        //Wenn die Werte übereinstimmen, ist die gesamte Position storniert und darf nicht in die Excel Datei geschrieben werden
                        if (entry.Quantity == entry.CanceledOrReturnedQuantity)
                        {
                            continue;
                        }

                        currentRow++;
                        worksheet.Cell(currentRow, 1).Value = _customerSettings.CustomerNumber;
                        worksheet.Cell(currentRow, 2).Value = _customerSettings.CustomerIln;
                        worksheet.Cell(currentRow, 3).Value = order.Code;
                        worksheet.Cell(currentRow, 4).Value = entry.DeliveryAddress?.FirstName + " " +
                                                              entry.DeliveryAddress?.LastName;

                        if (entry.DeliveryAddress != null &&
                            entry.DeliveryAddress.Type.Equals(SharedStatus.Default))
                        {
                            worksheet.Cell(currentRow, 5).Value = entry.DeliveryAddress?.Remarks;
                            worksheet.Cell(currentRow, 6).Value = entry.DeliveryAddress?.StreetName + " " +
                                                                  entry.DeliveryAddress?.StreetNumber;
                        }
                        else if (entry.DeliveryAddress != null &&
                                 entry.DeliveryAddress.Type.Equals(SharedStatus.Packstation))
                        {
                            worksheet.Cell(currentRow, 5).Value = entry.DeliveryAddress?.PostNumber;
                            worksheet.Cell(currentRow, 6).Value = entry.DeliveryAddress?.PackstationNumber;
                        }
                        else if (entry.DeliveryAddress != null &&
                                 entry.DeliveryAddress.Type.Equals(SharedStatus.PostOffice))
                        {
                            worksheet.Cell(currentRow, 5).Value =
                                entry.DeliveryAddress?.PostNumber ?? string.Empty;
                            worksheet.Cell(currentRow, 6).Value = entry.DeliveryAddress?.PostOfficeNumber;
                        }


                        worksheet.Cell(currentRow, 7).Value = entry.DeliveryAddress?.CountryIsoCode;
                        worksheet.Cell(currentRow, 8).Value = entry.DeliveryAddress?.PostalCode;
                        worksheet.Cell(currentRow, 9).Value = entry.DeliveryAddress?.Town;
                        worksheet.Cell(currentRow, 10).Value = order.Phone;
                        worksheet.Cell(currentRow, 11).Value =
                            entry.Quantity -
                            entry
                                .CanceledOrReturnedQuantity; //Wenn teile storniert wurden, muss das berücksichtig werden
                        worksheet.Cell(currentRow, 12).Value = entry.VendorProductCode;
                        worksheet.Cell(currentRow, 13).Value = entry.ProductName;
                    }
                }


                using (var stream = new MemoryStream())
                {
                    _excelWorkbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Es ist ein unerwarteter Fehler beim Erstellen der Excel Datei aufgetreten.");
                return new byte[0];
            }
        }

        public string SaveFileOnServer(byte[] content)
        {
            var uploadsFolder = _fileSettings.UploadFolder;

            try
            {
                var fileId = _guidGenerator.NewGuid().ToString();

                if (!_fileWrapper.DirectoryExists(uploadsFolder))
                {
                    _fileWrapper.CreateDirectory(uploadsFolder);
                }

                var fileName = $"Aldi_Export_{fileId}.xls";
                var filePath = Path.Combine(uploadsFolder, fileName);
                _fileWrapper.WriteAllBytes(filePath, content);

                _fileMapping.SetFilePath(fileId, filePath);

                // Log the file path to ensure it is saved correctly
                _logger.LogInformation($"Datei wurde unter dem Pfad '{filePath}' mit der fileId '{fileId}' gespeichert.");

                return fileId;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Fehler beim Speichern der Datei im Verzeichnis {UploadsFolder}", uploadsFolder);
                throw new FileSaveException($"Ein Fehler ist aufgetreten beim Speichern der Datei im Verzeichnis {uploadsFolder}.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Zugriff verweigert beim Speichern der Datei im Verzeichnis {UploadsFolder}", uploadsFolder);
                throw new FileSaveException($"Zugriff verweigert beim Speichern der Datei im Verzeichnis {uploadsFolder}.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ein unerwarteter Fehler ist aufgetreten beim Speichern der Datei.");
                throw new FileSaveException("Ein unerwarteter Fehler ist aufgetreten beim Speichern der Datei.", ex);
            }
        }

        public string GetFilePathByFileId(string fileId)
        {
            var filePath = _fileMapping.GetFilePath(fileId);
            if (string.IsNullOrEmpty(filePath))
            {
                _logger.LogError($"Der filePath mit der FileId '{fileId}' ist null.");
                return string.Empty;
            }
            return filePath;
        }

        public MemoryStream GeneratePdf(Return? returnObj)
        {
            if (returnObj == null)
            {
                _logger.LogError("Die Pdf kann nicht erzeugt werden, weil das Return Objekt null ist.");
                throw new ReturnIsNullException();
            }

            GlobalFontSettings.FontResolver = _fontResolver;

            MemoryStream stream = new MemoryStream();

            try
            {

                PdfDocument document = new PdfDocument();
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                XFont font = new XFont("Arial", 12);
                XFont boldFont = new XFont("Arial_bold", 12, XFontStyleEx.Bold);

                int yPosition = 40;

                gfx.DrawString("Aldi Retourendetails", boldFont, XBrushes.Black,
                    new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 40;
                gfx.DrawString($"Bestellcode: {returnObj.OrderCode}", font, XBrushes.Black,
                    new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString($"Retoure Erstellt: {returnObj.InitiationDate.ToShortDateString()}", font,
                    XBrushes.Black,
                    new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 40;
                gfx.DrawString(
                    $"{returnObj.CustomerInfo.Address?.FirstName} {returnObj.CustomerInfo.Address?.LastName}",
                    font, XBrushes.Black, new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString(
                    $"{returnObj.CustomerInfo.Address?.StreetName} {returnObj.CustomerInfo.Address?.StreetNumber}",
                    font, XBrushes.Black, new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString($"{returnObj.CustomerInfo.Address?.PostalCode} {returnObj.CustomerInfo.Address?.Town}",
                    font,
                    XBrushes.Black, new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 20;
                gfx.DrawString($"{returnObj.CustomerInfo.Address?.CountryIsoCode}", font, XBrushes.Black,
                    new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                yPosition += 40;

                foreach (var entry in returnObj.ReturnEntries)
                {
                    var orderEntry =
                        returnObj.Order.Entries.FirstOrDefault(e => e.EntryNumber == entry.OrderEntryNumber);

                    foreach (var consignment in entry.ReturnConsignments)
                    {
                        foreach (var package in consignment.Packages)
                        {
                            gfx.DrawString($"Trackingnr.: {package.TrackingId}", font, XBrushes.Black,
                                new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                            yPosition += 20;
                            gfx.DrawString($"Artikelnr.: {orderEntry?.VendorProductCode}", font, XBrushes.Black,
                                new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                            yPosition += 20;
                            gfx.DrawString($"Produktname: {orderEntry?.ProductName}", font, XBrushes.Black,
                                new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                            yPosition += 20;
                            gfx.DrawString($"Menge: {consignment.Quantity}", font, XBrushes.Black,
                                new XRect(40, yPosition, page.Width, page.Height), XStringFormats.TopLeft);
                            yPosition += 20;
                            gfx.DrawString($"Grund: {ReasonCodeMapping.GetReasonDescription(entry.Reason)}", font,
                                XBrushes.Black, new XRect(40, yPosition, page.Width, page.Height),
                                XStringFormats.TopLeft);
                            yPosition += 40;
                        }
                    }
                }

                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/aldi.png");
                XImage logo = _imageLoader.LoadImage(logoPath);

                double originalLogoWidth = 3000;
                double originalLogoHeight = 2120;
                double targetLogoHeight = 150;

                double scaleFactor = targetLogoHeight / originalLogoHeight;

                double targetLogoWidth = originalLogoWidth * scaleFactor;

                double logoX = page.Width - targetLogoWidth - 40;
                double logoY = 40;

                gfx.DrawImage(logo, logoX, logoY, targetLogoWidth, targetLogoHeight);

                document.Save(stream, false);
                document.Close();

            } 
            catch (Exception ex)
            {
                throw new PdfGenerationException("Fehler bei PDF-Generierung.", ex);
            }

            return stream;
        }
    }