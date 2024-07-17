using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace Shared.Services;

public class ReturnConsignmentAndPackageService : IReturnConsignmentAndPackageService
{
    private readonly ILogger<ReturnConsignmentAndPackageService> _logger;
    private readonly IReturnRepository _returnRepository;
    private readonly IValidatorWrapper<ReceivingReturnResponse> _receivingReturnResponseValidator;
    private readonly IValidatorWrapper<Return> _returnValidator;


    public ReturnConsignmentAndPackageService(ILogger<ReturnConsignmentAndPackageService> logger,
        IReturnRepository returnRepository, IValidatorWrapper<ReceivingReturnResponse> receivingReturnResponseValidator,
        IValidatorWrapper<Return> returnValidator)
    {
        _logger = logger;
        _returnRepository = returnRepository;
        _receivingReturnResponseValidator = receivingReturnResponseValidator;
        _returnValidator = returnValidator;
    }

    public async Task UpdateAllReturnPackageStatusFromReturnAsync(string packageStatus, Return returnObj)
    {
        try
        {
            foreach (var returnEntry in returnObj.ReturnEntries)
            {
                foreach (var returnConsignment in returnEntry.ReturnConsignments)
                {
                    foreach (var returnPackage in returnConsignment.Packages)
                    {
                        _logger.LogInformation(
                            $"Es wird versucht den Status der ReturnPackage mit der Id '{returnPackage.Id}' in der Datenbank auf '{packageStatus}' zu aktualisiert.");

                        await _returnRepository.UpdateReturnPackageStatusAsync(packageStatus, returnPackage.Id);

                        _logger.LogInformation(
                            $"Der Status der ReturnPackage mit der Id '{returnPackage.Id}' wurde in der Datenbank erfolgreich auf '{packageStatus}' aktualisiert.");
                    }
                }
            }
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren des Status '{packageStatus}' für die ReturnPackages mit der Return Id '{returnObj.Id}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim aktualisieren des Status '{packageStatus}' für die ReturnPackages mit der Return Id '{returnObj.Id}'.");
            throw new ReturnConsignmentAndPackageServiceException(
                $"Unerwarteter Fehler beim aktualisieren des Status '{packageStatus}' für die ReturnPackages mit der Return Id '{returnObj.Id}'.",
                ex);
        }
    }

    public async Task UpdateReturnConsignmentStatusQuantityAsync(string packageStatus, Return returnObj)
    {
        foreach (var returnEntry in returnObj.ReturnEntries)
        {
            foreach (var returnConsignment in returnEntry.ReturnConsignments)
            {
                _logger.LogInformation(
                    $"Es wird versucht die {packageStatus} Quantity der ReturnConsignment mit dem ConsignmentCode '{returnConsignment.ConsignmentCode}' zu aktualisiert.");

                try
                {
                    await _returnRepository.UpdateReturnConsignmentStatusQuantityAsync(packageStatus,
                        returnConsignment.Quantity, returnConsignment.ConsignmentCode);
                }

                catch (RepositoryException ex)
                {
                    _logger.LogError(ex,
                        $"Repository-Exception beim aktualisieren der {packageStatus} Quantity für das ReturnConsignment mit dem ConsignmentCode '{returnConsignment.ConsignmentCode}'.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Unerwarteter Fehler beim aktualisieren der {packageStatus} Quantity für das ReturnConsignment mit dem ConsignmentCode '{returnConsignment.ConsignmentCode}'.");
                    throw new ReturnConsignmentAndPackageServiceException(
                        $"Unerwarteter Fehler beim aktualisieren der {packageStatus} Quantity für das ReturnConsignment mit dem ConsignmentCode '{returnConsignment.ConsignmentCode}'.",
                        ex);
                }

                _logger.LogInformation(
                    $"Die {packageStatus} Quantity der ReturnConsignment mit dem ConsignmentCode '{returnConsignment.ConsignmentCode}' wurde in der Datenbank erfolgreich aktualisiert.");
            }
        }
    }

    public async Task UpdateCompletedDateForAllReturnConsignments(Return returnObj)
    {
        try
        {
            foreach (var returnEntry in returnObj.ReturnEntries)
            {
                foreach (var returnConsignment in returnEntry.ReturnConsignments)
                {
                    returnConsignment.CompletedDate = DateTime.UtcNow;
                    await UpdateReturnConsignmentAsync(returnConsignment);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Fehler beim aktualisieren des CompletedDate für alle ReturnConsignment der Return mit der Id '{returnObj.Id}'.");
            throw new ReturnConsignmentAndPackageServiceException(
                $"Fehler beim aktualisieren des CompletedDate für alle ReturnConsignment der Return mit der Id '{returnObj.Id}'.",
                ex);
        }
    }


    public async Task UpdateReturnConsignmentAsync(ReturnConsignment returnConsignment)
    {
        try
        {
            await _returnRepository.UpdateReturnConsignmentAsync(returnConsignment);
            _logger.LogInformation(
                $"Das ReturnConsignment mit der Id '{returnConsignment.Id}' wurde erfolgreich in der Datenbank aktualisiert.");
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim aktualisieren der ReturnConsignment mit der Id '{returnConsignment.Id}'.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim beim aktualisieren der ReturnConsignment mit der Id '{returnConsignment.Id}'.");
            throw new ReturnConsignmentAndPackageServiceException(
                $"Unerwarteter Fehler beim beim aktualisieren der ReturnConsignment mit der Id '{returnConsignment.Id}'.",
                ex);
        }
    }

    public async Task<Return> CreateReturnConsignmentAndReturnPackage(
        ReceivingReturnResponse parsedReceivingReturnResponse,
        Return returnObj)
    {
        _logger.LogInformation(
            $"Es wird versucht, Versanformationen in der Datenbank für die Retoure mit dem AldiReturnCode '{returnObj.AldiReturnCode}' zu erstellen.");

        try
        {
            await _receivingReturnResponseValidator.ValidateAndThrowAsync(parsedReceivingReturnResponse);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für ReceivingReturnResponse: {ex.Message}");
        }

        foreach (var returnEntry in returnObj.ReturnEntries)
        {
            var parsedEntry =
                parsedReceivingReturnResponse.entries.FirstOrDefault(e => e.entryCode == returnEntry.EntryCode);

            if (parsedEntry != null)
            {
                returnEntry.ReturnConsignments = CreateReturnConsignments(parsedEntry);
            }
        }

        try
        {
            await _returnValidator.ValidateAndThrowAsync(returnObj);
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, $"Validierungsfehler für Return: {ex.Message}");
            string returnObjJson = JsonConvert.SerializeObject(returnObj, Formatting.Indented);
            _logger.LogInformation($"Daten des returnObj: {returnObjJson}");
            throw;
        }

        return returnObj;
    }

    private List<ReturnConsignment> CreateReturnConsignments(ReceivingReturnEntriesResponse parsedEntry)
    {
        var returnConsignments = new List<ReturnConsignment>();

        foreach (var consignment in parsedEntry.consignments)
        {
            var returnConsignment = new ReturnConsignment()
            {
                ConsignmentCode = consignment.consignmentCode,
                Quantity = consignment.quantity,
                Carrier = consignment.carrier,
                CarrierCode = consignment.carrierCode,
                Status = SharedStatus.Receiving,
                Packages = CreateReturnPackages(consignment.packages)
            };

            returnConsignments.Add(returnConsignment);
        }

        return returnConsignments;
    }

    private List<ReturnPackage> CreateReturnPackages(ICollection<ReceivingReturnPackagesResponse> packages)
    {
        return packages.Select(p => new ReturnPackage()
        {
            VendorPackageCode = p.vendorPackageCode,
            TrackingId = p.trackingId,
            TrackingLink = p.trackingLink,
            Status = p.status
        }).ToList();
    }
    
    public async Task<ReturnConsignment> GetReturnConsignmentByConsignmentCodeAsync(string consignmentCode)
    {
        if (string.IsNullOrEmpty(consignmentCode))
        {
            _logger.LogError($"'{nameof(consignmentCode)}' darf nicht null sein.");
            throw new ReturnConsignmentAndPackageServiceArgumentException($"'{nameof(consignmentCode)}' darf nicht null sein");
        }

        try
        {
            var returnConsignment = await _returnRepository.GetReturnConsignmentByConsignmentCodeAsync(consignmentCode);

            if (returnConsignment == null)
            {
                _logger.LogError($"Es wurde kein ReturnConsignment mit dem ConsignmentCode '{consignmentCode}' in der Datenbank gefunden.");
                throw new ReturnConsignmentNotFoundException(
                    $"Es wurde kein ReturnConsignment mit dem ConsignmentCode '{consignmentCode}' in der Datenbank gefunden.");
            }

            return returnConsignment;
        }
        catch (RepositoryException ex)
        {
            _logger.LogError(ex,
                $"Repository-Exception beim Abrufen von ReturnConsignment mit dem ConsignmentCode '{consignmentCode}'.");
            throw;
        }
        catch (ReturnConsignmentNotFoundException ex)
        {
            _logger.LogWarning(ex.Message);
            throw;
        }
        catch (ReturnConsignmentAndPackageServiceArgumentException ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                $"Unerwarteter Fehler beim Abrufen von ReturnConsignment mit dem ConsignmentCode '{consignmentCode}'.");
            throw new ReturnConsignmentAndPackageServiceException(
                $"Unerwarteter Fehler beim Abrufen von ReturnConsignment mit dem ConsignmentCode '{consignmentCode}'.",
                ex);
        }
    }
}