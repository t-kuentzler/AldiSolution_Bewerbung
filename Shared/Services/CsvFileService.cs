using System.Globalization;
using System.Runtime.InteropServices;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Entities;
using Shared.Models;

namespace Shared.Services;

public class CsvFileService : ICsvFileService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CsvFileService> _logger;
    private readonly IOrderService _orderService;

    public CsvFileService(IConfiguration configuration, ILogger<CsvFileService> logger,
        IOrderService orderService)
    {
        _configuration = configuration;
        _logger = logger;
        _orderService = orderService;
    }

    public List<ConsignmentFromCsv> GetConsignmentsFromCsvFiles()
    {
        List<ConsignmentFromCsv> csvConsignments = new List<ConsignmentFromCsv>();
        try
        {
            string folderPath = GetFolderPath();

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                _logger.LogError($"Der Ordner unter '{folderPath}' wurde nicht gefunden oder der Pfad ist leer.");
                throw new DirectoryNotFoundException(
                    $"Der Ordner unter '{folderPath}' wurde nicht gefunden oder der Pfad ist leer.");
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
                MissingFieldFound = null
            };

            var csvFiles = Directory.GetFiles(folderPath, "*.csv");

            foreach (var filePath in csvFiles)
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, config))
                {
                    var records = csv.GetRecords<ConsignmentFromCsv>().ToList();
                    csvConsignments.AddRange(records);
                }
            }
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (CsvHelperException ex)
        {
            _logger.LogError($"Fehler beim Parsen der CSV-Datei: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Ein unerwarteter Fehler ist aufgetreten: {ex.Message}");
            throw;
        }

        return csvConsignments;
    }

    private string GetFolderPath()
    {
        string folderPathWindows = _configuration.GetValue<string>("CsvConsignmentPath:Windows") ?? String.Empty;
        string folderPathMac = _configuration.GetValue<string>("CsvConsignmentPath:MacOS") ?? String.Empty;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(folderPathWindows))
        {
            return folderPathWindows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !string.IsNullOrEmpty(folderPathMac))
        {
            return folderPathMac;
        }
        else
        {
            throw new NotSupportedException(
                "Das Betriebssystem wird nicht unterstützt oder der Pfad ist nicht konfiguriert.");
        }
    }

    public async Task<List<Consignment>> ParseConsignmentsFromCsvToConsignments(
        List<ConsignmentFromCsv> csvConsignments)
    {
        //Sicherheitsprüfung, dass nur Datensätze mit Aldi Kundennummer verarbeitet werden
        var customerNumber = _configuration.GetValue<string>("CustomerSettings:CustomerNumber");
        var filteredCsvConsignmentsByKdnr = csvConsignments.Where(x => x.kdnr == customerNumber).ToList();

        if (filteredCsvConsignmentsByKdnr.Count == 0)
        {
            _logger.LogWarning(
                $"Es wurden keine Datensätze in der CSV Datei mit der kdnr '{customerNumber}' gefunden.");
            return new List<Consignment>();
        }

        var groupedByTrackingNumber = filteredCsvConsignmentsByKdnr
            .GroupBy(x => x.nve_nr)
            .ToList();

        var consignments = new List<Consignment>();

        foreach (var group in groupedByTrackingNumber)
        {
            var consignmentEntries = new List<ConsignmentEntry>();
            var firstGroupEntry = group.FirstOrDefault();

            if (firstGroupEntry == null)
            {
                _logger.LogError("Die Gruppe hat keinen ersten Eintrag.");
                continue;
            }

            var order = await _orderService.GetOrderByOrderCodeAsync(firstGroupEntry.kontrakt_nr_kunde);


            foreach (var csvEntry in group)
            {
                //In der CSV wird Artikelnummer ohne Farbcode ausgegeben
                var articleNumber = $"{csvEntry.artikelnummer}{csvEntry.farbe_id}";
                var orderEntry = order?.Entries.FirstOrDefault(oe => 
                    oe.VendorProductCode.Replace(" ", "").Equals(articleNumber.Replace(" ", ""))
                );
                if (orderEntry == null)
                {
                    _logger.LogWarning(
                        $"Es wurde kein orderEntry mit dem OrderCode '{csvEntry.kontrakt_nr_kunde}' und der Artikelnummer '{articleNumber}' gefunden. Der Datensatz wird übersprungen.");
                    continue;
                }

                consignmentEntries.Add(new ConsignmentEntry
                {
                    OrderEntryId = orderEntry.Id,
                    OrderEntryNumber = orderEntry.EntryNumber,
                    Quantity = Convert.ToInt32(csvEntry.menge)
                });
            }

            if (order == null)
            {
                _logger.LogWarning(
                    $"Es wurde keine Order mit dem OrderCode '{firstGroupEntry.kontrakt_nr_kunde}' in der Datenbank gefunden.");
                continue;
            }

            // Überprüfen, ob überhaupt Einträge für das Konsignment erzeugt wurden.
            if (consignmentEntries.Any())
            {
                var firstEntry = group.First();
                var consignment = new Consignment
                {
                    Carrier = firstEntry.paket,
                    ShippingDate = DateTime.Parse(firstEntry.datum_druck),
                    Status = "SHIPPED",
                    StatusText = "versendet.",
                    TrackingId = firstEntry.nve_nr,
                    VendorConsignmentCode = firstEntry.verpackungs_nr,
                    OrderCode = firstEntry.kontrakt_nr_kunde,
                    Order = order,
                    ConsignmentEntries = consignmentEntries
                };

                if (consignment.Carrier.Equals("DPD"))
                {
                    consignment.TrackingLink = $"https://tracking.dpd.de/parcelstatus?query={consignment.TrackingId}";
                }
                else if (consignment.Carrier.Equals("DHL"))
                {
                    consignment.TrackingLink =
                        $"https://www.dhl.de/de/privatkunden/dhl-sendungsverfolgung.html?piececode={consignment.TrackingId}";
                }

                var firstOrderEntry = order.Entries?.FirstOrDefault();
                if (firstOrderEntry != null && firstOrderEntry.DeliveryAddress != null)
                {
                    var shippingAddress = new ShippingAddress
                    {
                        Type = firstOrderEntry.DeliveryAddress.Type,
                        SalutationCode = firstOrderEntry.DeliveryAddress.SalutationCode,
                        FirstName = firstOrderEntry.DeliveryAddress.FirstName,
                        LastName = firstOrderEntry.DeliveryAddress.LastName,
                        StreetName = firstOrderEntry.DeliveryAddress.StreetName,
                        StreetNumber = firstOrderEntry.DeliveryAddress.StreetNumber,
                        Remarks = firstOrderEntry.DeliveryAddress.Remarks,
                        PostalCode = firstOrderEntry.DeliveryAddress.PostalCode,
                        Town = firstOrderEntry.DeliveryAddress.Town,
                        PackstationNumber = firstOrderEntry.DeliveryAddress.PackstationNumber,
                        PostNumber = firstOrderEntry.DeliveryAddress.PostNumber,
                        PostOfficeNumber = firstOrderEntry.DeliveryAddress.PostOfficeNumber,
                        CountryIsoCode = firstOrderEntry.DeliveryAddress.CountryIsoCode
                    };

                    consignment.ShippingAddress = shippingAddress;
                }


                consignments.Add(consignment);
            }
        }

        return consignments;
    }


    public void MoveCsvFilesToArchiv()
    {
        try
        {
            string sourceFolderPath = GetFolderPath();
            string archiveFolderPath = Path.Combine(sourceFolderPath, "Archiv");

            if (!Directory.Exists(archiveFolderPath))
            {
                Directory.CreateDirectory(archiveFolderPath);
            }

            var csvFiles = Directory.GetFiles(sourceFolderPath, "*.csv");

            foreach (var filePath in csvFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var destinationFilePath = Path.Combine(archiveFolderPath, fileName);

                File.Move(filePath, destinationFilePath, true);
                _logger.LogInformation($"Datei '{fileName}' wurde erfolgreich ins Archiv verschoben.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fehler beim Verschieben der CSV-Dateien ins Archiv: {ex.Message}");
        }
    }
}