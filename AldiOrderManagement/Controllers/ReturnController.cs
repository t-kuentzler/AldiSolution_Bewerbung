using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Mappings;
using Shared.Models;


namespace AldiOrderManagement.Controllers;

public class ReturnController : Controller
{
    private readonly IReturnService _returnService;
    private readonly ILogger<ReturnController> _logger;
    private readonly IFileService _fileService;

    public ReturnController(IReturnService returnService, ILogger<ReturnController> logger, 
        IFileService fileService)
    {
        _returnService = returnService;
        _logger = logger;
        _fileService = fileService;
    }

    public async Task<IActionResult> CreateReturn(int orderId, Dictionary<int, ReturnEntryModel> returnEntries)
    {
        try
        {
            var result = await _returnService.ProcessReturn(orderId, returnEntries);
            if (result.Success)
            {
                await _returnService.ProcessManualReturnAsync(orderId, result.ManualReturnResponse, returnEntries);

                TempData["SuccessMessage"] = "Die Retoure wurde erfolgreich erstellt.";
                return RedirectToAction("OrderDetailsWithReturn", "Order", new { id = orderId });
            }

            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Fehler beim Erstellen der Retoure: " + ex.Message;
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
        }

        return RedirectToAction("OrderDetailsWithReturn", "Order", new { id = orderId });
    }

    public async Task<IActionResult> RetrievedReturns(SearchTerm searchTerm)
    {
        string status = SharedStatus.InProgress;

        try
        {
            IEnumerable<Return> retrievedReturns;
            var statuses = new List<string> { status };

            if (!string.IsNullOrWhiteSpace(searchTerm.value))
            {
                // Suchlogik
                retrievedReturns = await _returnService.SearchReturnsAsync(searchTerm, statuses);
            }
            else
            {
                retrievedReturns = await _returnService.GetAllReturnsByStatusesAsync(statuses);
            }

            ViewBag.SearchTerm = searchTerm.value;

            var returnList = retrievedReturns.OrderByDescending(r => r.InitiationDate).ToList();
            return View(returnList);
        }
        catch (ValidationException ex)
        {
            var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
            TempData["ErrorMessage"] = errorMessages;
            return View(new List<Return>());
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = "Fehler beim Laden der offenen Retouren: " + ex.Message;
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }

    public async Task<IActionResult> ReceivingReturns(SearchTerm searchTerm)
    {
        try
        {
            IEnumerable<Return> receivingReturns;
            var statuses = new List<string> { SharedStatus.Received, SharedStatus.Receiving };

            if (!string.IsNullOrWhiteSpace(searchTerm.value))
            {
                // Suchlogik
                receivingReturns = await _returnService.SearchReturnsAsync(searchTerm, statuses);
            }
            else
            {
                receivingReturns = await _returnService.GetAllReturnsByStatusesAsync(statuses);
            }

            ViewBag.SearchTerm = searchTerm.value;

            var returnList = receivingReturns.OrderByDescending(r => r.InitiationDate).ToList();
            return View(returnList);
        }
        catch (ValidationException ex)
        {
            var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
            TempData["ErrorMessage"] = errorMessages;
            return View(new List<Return>());
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = "Fehler beim Laden der erwarteten Retouren: " + ex.Message;
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }

    public async Task<IActionResult> CompletedReturns(SearchTerm searchTerm, bool all = false)
    {
        string status = SharedStatus.Completed;
        var statuses = new List<string> { status };

        try
        {
            IEnumerable<Return> completedReturns;

            if (!string.IsNullOrWhiteSpace(searchTerm.value))
            {
                // Suchlogik
                completedReturns = await _returnService.SearchReturnsAsync(searchTerm, statuses);
            }
            else
            {
                completedReturns = await _returnService.GetAllReturnsByStatusesAsync(statuses);
            }

            var totalCompletedReturnsCount = completedReturns.Count();  // Gesamtanzahl der abgeschlossenen Retouren ermitteln
            ViewBag.TotalCompletedReturnsCount = totalCompletedReturnsCount;
            
            ViewBag.SearchTerm = searchTerm.value;

            var returnList = completedReturns.OrderByDescending(r => r.InitiationDate).ToList();
            
            if (!all)
            {
                returnList = returnList.Take(20).ToList();
            }
            
            return View(returnList);
        }
        catch (ValidationException ex)
        {
            var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
            TempData["ErrorMessage"] = errorMessages;
            return View(new List<Return>());
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = "Fehler beim Laden der abgeschlossenen Retouren: " + ex.Message;
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }

    public async Task<IActionResult> ReturnDetails(int id)
    {
        try
        {
            var returnObj = await _returnService.GetReturnByIdAsync(id);

            if (returnObj == null)
            {
                return NotFound();
            }

            foreach (var entry in returnObj.ReturnEntries)
            {
                entry.Reason = ReasonCodeMapping.GetReasonDescription(entry.Reason);
            }

            var shipmentInfos = _returnService.CreateShipmentInfos(returnObj);

            ReturnDetailsViewModel returnDetailsViewModel = new ReturnDetailsViewModel()
            {
                returnObj = returnObj,
                ShipmentInfos = shipmentInfos
            };

            return View(returnDetailsViewModel);
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = $"Fehler beim Abrufen von Retourendetails: {ex.Message}";
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }


    [HttpPost]
    public async Task<IActionResult> CreateShipmentInfo([FromBody] ShipmentInfoAndReturnIdRequest request)
    {
        try
        {
            await _returnService.ProcessShipmentInfoCreation(request);
            TempData["SuccessMessage"] = "Die Versandinformationen wurden erfolgreich erstellt.";
            return Ok();
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ReturnIsNullException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            _logger.LogError(ex, "Ein Fehler ist während der Verarbeitung aufgetreten.");
            return StatusCode(500, "Ein interner Serverfehler ist aufgetreten.");
        }
    }


    [HttpPost]
    public async Task<IActionResult> MarkConsignmentAsReceived([FromBody] MarkConsignmentAsReceivedRequest request)
    {
        if (string.IsNullOrEmpty(request.ConsignmentCode))
        {
            return BadRequest("ConsignmentCode ist erforderlich.");
        }

        await _returnService.UpdateReturnConsignmentAndPackagesStatusAsync(request.ConsignmentCode,
            SharedStatus.Received);
        await _returnService.UpdateReturnPackagesReceiptDeliveryAsync(request.ConsignmentCode);

        bool allConsignmentsAreReceived = await _returnService.CheckIfAllConsignmentsAreReceived(request.ReturnId);
        if (allConsignmentsAreReceived)
        {
            await _returnService.UpdateReturnStatusAsync(request.ReturnId, SharedStatus.Received);
        }


        return Ok();
    }

    public async Task<IActionResult> ReturnDetailsWithCompletion(int id, bool updateSuccess = false)
    {
        try
        {
            var returnObj = await _returnService.GetReturnByIdAsync(id);

            if (returnObj == null)
            {
                return NotFound();
            }

            foreach (var entry in returnObj.ReturnEntries)
            {
                entry.Reason = ReasonCodeMapping.GetReasonDescription(entry.Reason);
            }

            if (updateSuccess)
            {
                TempData["SuccessMessage"] = "Die Informationen wurden erfolgreich aktualisiert.";
            }

            return View(returnObj);
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = $"Fehler beim Abrufen von Retourendetails: {ex.Message}";
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }

    
    [HttpPost]
    public async Task<IActionResult> UpdatePackageStatus([FromBody] PackageStatusUpdateRequest request)
    {
        try
        {
            await _returnService.ProcessPackageStatusUpdateAsync(request);
            return Ok();
        }
        catch (ArgumentNullException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    public async Task<IActionResult> CompletedReturnDetails(int id)
    {
        try
        {
            var returnObj = await _returnService.GetReturnByIdAsync(id);

            if (returnObj == null)
            {
                return NotFound();
            }

            foreach (var entry in returnObj.ReturnEntries)
            {
                entry.Reason = ReasonCodeMapping.GetReasonDescription(entry.Reason);
            }

            return View(returnObj);
        }
        catch (ReturnServiceException ex)
        {
            TempData["ErrorMessage"] = $"Fehler beim Abrufen von Retourendetails: {ex.Message}";
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
            return View();
        }
    }

    public async Task<IActionResult> GeneratePdf(int returnId)
    {
        try
        {
            var returnObj = await _returnService.GetReturnByIdAsync(returnId);
            if (returnObj == null)
            {
                _logger.LogWarning($"Kein Rückgabeobjekt für die returnId '{returnId}' gefunden.");
                return NotFound($"Kein Rückgabeobjekt für die returnId '{returnId}' gefunden.");
            }

            var pdfStream = _fileService.GeneratePdf(returnObj);
            return File(pdfStream.ToArray(), "application/pdf", "RetoureDetails.pdf");
        }
        catch (ReturnIsNullException ex)
        {
            _logger.LogError(ex,
                "Die PDF-Generierung konnte nicht durchgeführt werden, da das Rückgabeobjekt null ist.");
            return BadRequest("Die PDF-Generierung konnte nicht durchgeführt werden.");
        }
        catch (PdfGenerationException ex)
        {
            _logger.LogError(ex, "Ein unerwarteter Fehler ist bei der Erstellung des PDFs aufgetreten.");
            return StatusCode(500, "Ein Fehler ist bei der Erstellung des PDFs aufgetreten.");
        }
    }
}