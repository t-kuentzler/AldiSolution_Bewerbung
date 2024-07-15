using System.Globalization;
using System.Text.RegularExpressions;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;
using Microsoft.Extensions.Logging;

namespace Shared.Services
{
    public class ReturnService : IReturnService
    {
        private readonly ILogger<ReturnService> _logger;
        private readonly IReturnRepository _returnRepository;
        private readonly IRmaNumberGenerator _rmaNumberGenerator;

        public ReturnService(ILogger<ReturnService> logger, IReturnRepository returnRepository,
            IRmaNumberGenerator rmaNumberGenerator)
        {
            _logger = logger;
            _returnRepository = returnRepository;
            _rmaNumberGenerator = rmaNumberGenerator;
        }

        public List<Return> ParseReturnResponseToReturnObject(ReturnResponse returnResponse)
        {
            List<Return> returnList = new List<Return>();

            foreach (var returnRequest in returnResponse.ReturnRequests)
            {
                var returnObj = new Return
                {
                    OrderCode = returnRequest.OrderCode,
                    InitiationDate = returnRequest.InitiationDate,
                    AldiReturnCode = returnRequest.Code,
                    Status = SharedStatus.InProgress,
                    Rma = _rmaNumberGenerator.GenerateRma(returnRequest.OrderCode),
                
                    CustomerInfo = new CustomerInfo
                    {
                        EmailAddress = returnRequest.CustomerEmailAddress,
                        PhoneNumber = returnRequest.CustomerPhoneNumber,
                        Address = new Address
                        {
                            Type = returnRequest.Address.Type,
                            SalutationCode = returnRequest.Address.SalutationCode ?? string.Empty,
                            FirstName = returnRequest.Address.FirstName,
                            LastName = returnRequest.Address.LastName,
                            StreetName = returnRequest.Address.StreetName,
                            StreetNumber = returnRequest.Address.StreetNumber,
                            Remarks = returnRequest.Address.Remarks ?? string.Empty,
                            PostalCode = returnRequest.Address.PostalCode,
                            Town = returnRequest.Address.Town,
                            PackstationNumber = returnRequest.Address.PackstationNumber ?? string.Empty,
                            PostNumber = returnRequest.Address.PostNumber ?? string.Empty,
                            PostOfficeNumber = returnRequest.Address.PostOfficeNumber ?? string.Empty,
                            CountryIsoCode = returnRequest.Address.CountryIsoCode
                        }
                    },

                    ReturnEntries = new List<ReturnEntry>()
                };

                foreach (var requestEntry in returnRequest.Entries)
                {
                    var entry = new ReturnEntry
                    {
                        Reason = requestEntry.Reason,
                        Notes = CleanNotes(requestEntry.Notes),
                        Status = SharedStatus.InProgress,
                        OrderEntryNumber = requestEntry.OrderEntryNumber,
                        Quantity = requestEntry.Quantity,
                        EntryCode = requestEntry.Code,
                        CarrierCode = requestEntry.CarrierCode
                    };

                    returnObj.ReturnEntries.Add(entry);
                }

                returnList.Add(returnObj);
            }

            return returnList;
        }
    
        // Sonderzeichen entfernen, da API diese nicht akzeptiert
        private string CleanNotes(string? notes)
        {
            if (notes == null)
                return string.Empty;

            return Regex.Replace(notes, @"[^a-zA-Z0-9\s]", "");
        }
    
        public async Task<bool> CreateReturnAsync(Return returnObj)
        {
            try
            {
                _logger.LogInformation(
                    $"Es wird versucht, die Retoure mit dem AldiReturnCode '{returnObj.AldiReturnCode}' in der Datenbank zu erstellen.");
                await _returnRepository.CreateReturnAsync(returnObj);
                _logger.LogInformation(
                    $"Die Retoure mit dem AldiReturnCode '{returnObj.AldiReturnCode}' wurde erfolgreich in der Datenbank erstellt.");
        
                return true; // Erfolg signalisieren
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Unerwarteter Fehler beim Erstellen der Retoure. Rma code: '{returnObj.Rma}', OrderCode: '{returnObj.OrderCode}', AldiReturnCode: '{returnObj.AldiReturnCode}'.");
                throw new ReturnServiceException(
                    $"Fehler beim Erstellen der Retoure. Rma code: '{returnObj.Rma}', OrderCode: '{returnObj.OrderCode}', AldiReturnCode: '{returnObj.AldiReturnCode}'.", ex);
            }
        }

        public ReturnInProgressRequest? ParseReturnToReturnInProgressRequest(Return returnObj)
        {
            if (returnObj.CustomerInfo.Address == null)
            {
                _logger.LogError("Address ist null.");
                return new ReturnInProgressRequest();
            }
        
            var request = new ReturnInProgressRequest()
            {
                aldiReturnCode = returnObj.AldiReturnCode,
                customerInfo = new ReturnInProgressCustomerInfoRequest()
                {
                    emailAddress = returnObj.CustomerInfo.EmailAddress,
                    address = new ReturnInProgressAddressRequest()
                    {
                        type = returnObj.CustomerInfo.Address.Type,
                        salutationCode = returnObj.CustomerInfo.Address.SalutationCode,
                        firstName = returnObj.CustomerInfo.Address.FirstName,
                        lastName = returnObj.CustomerInfo.Address.LastName,
                        streetName = returnObj.CustomerInfo.Address.StreetName ?? string.Empty,
                        streetNumber = returnObj.CustomerInfo.Address.StreetNumber ?? string.Empty,
                        town = returnObj.CustomerInfo.Address.Town,
                        postalCode = returnObj.CustomerInfo.Address.PostalCode,
                        packstationNumber = returnObj.CustomerInfo.Address.PackstationNumber,
                        remarks = returnObj.CustomerInfo.Address.Remarks,
                        postNumber = returnObj.CustomerInfo.Address.PostNumber,
                        postOfficeNumber = returnObj.CustomerInfo.Address.PostOfficeNumber,
                        countryIsoCode = returnObj.CustomerInfo.Address.CountryIsoCode,

                    }
                },
                entries = new List<ReturnInProgressEntryRequest>(),
                initiationDate = returnObj.InitiationDate.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                orderCode = returnObj.OrderCode
            };

            foreach (var entry in returnObj.ReturnEntries)
            {
                var entryRequest = new ReturnInProgressEntryRequest()
                {
                    entryCode = entry.EntryCode,
                    notes = entry.Notes ?? string.Empty,
                    orderEntryNumber = entry.OrderEntryNumber,
                    quantity = entry.Quantity,
                    reason = entry.Reason ?? string.Empty,
                    status = SharedStatus.InProgress
                
                };
            
                request.entries.Add(entryRequest);
            }

            return request;
        }
    }
}
