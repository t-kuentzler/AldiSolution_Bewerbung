using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace AldiOrderManagement.Controllers;

public class ConsignmentController : Controller
{
    private readonly IConsignmentService _consignmentService;
    private readonly IOrderService _orderService;
    private readonly IOAuthClientService _oAuthClientService;
    private readonly ILogger<ConsignmentController> _logger;

    public ConsignmentController(IConsignmentService consignmentService,
        IOrderService orderService, IOAuthClientService oAuthClientService,
        ILogger<ConsignmentController> logger)
    {
        _consignmentService = consignmentService;
        _orderService = orderService;
        _oAuthClientService = oAuthClientService;
        _logger = logger;
    }

    public async Task<IActionResult> ShippedConsignments(SearchTerm searchTerm)
    {
        string status = SharedStatus.Shipped;
        try
        {
            IEnumerable<Consignment> consignments;

            if (!string.IsNullOrWhiteSpace(searchTerm.value))
            {
                // Suchlogik
                consignments = await _consignmentService.SearchConsignmentsAsync(searchTerm, status);
            }
            else
            {
                consignments = await _consignmentService.GetAllConsignmentsByStatusAsync(status);
            }

            ViewBag.SearchTerm = searchTerm.value;

            var consignmentList = consignments.OrderByDescending(c => c.ShippingDate).ToList();
            return View(consignmentList);
        }
        catch (ValidationException ex)
        {
            var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
            TempData["ErrorMessage"] = errorMessages;
            return View(new List<Consignment>());
        }
        catch (ConsignmentServiceException ex)
        {
            TempData["ErrorMessage"] = "Fehler beim Laden der versandten Lieferungen: " + ex.Message;
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");

            return View();
        }
    }

    public async Task<IActionResult> ConsignmentDetails(int id)
    {
        try
        {
            var consignment = await _consignmentService.GetConsignmentByIdAsync(id);

            if (consignment == null)
            {
                return NotFound();
            }

            return View(consignment);
        }
        catch (ConsignmentServiceException ex)
        {
            TempData["ErrorMessage"] = $"Fehler beim Abrufen von Lieferungsdetails: {ex.Message}";
            return View();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");

            return View();
        }
    }
    
    public async Task<IActionResult> CancelConsignment(int id)
    {
        try
        {
            var consignment = await _consignmentService.GetConsignmentByIdAsync(id);

            if (consignment == null)
            {
                return NotFound();
            }

            bool apiResult = await _oAuthClientService.CancelConsignmentAfterDispatchAsync(consignment);

            if (apiResult)
            {
                await _consignmentService.UpdateConsignmentStatusAsync(SharedStatus.Cancelled, consignment);

                var order = await _orderService.GetOrderByOrderCodeAsync(consignment.OrderCode);
                bool allConsignmentsAreCancelled = _consignmentService.AreAllConsignmentsCancelled(order);

                if (allConsignmentsAreCancelled)
                {
                    await _orderService.UpdateOrderStatusByIdAsync(order.Id, SharedStatus.Canceled);
                }
                
                TempData["SuccessMessage"] = "Die Lieferung wurde erfolgreich storniert.";
                return RedirectToAction("ShippedConsignments");
            }
            return RedirectToAction("ShippedConsignments");

        }
        catch (ConsignmentServiceException ex)
        {
            TempData["ErrorMessage"] = "Fehler beim stornieren der Lieferung: " + ex.Message;
            return RedirectToAction("ShippedConsignments");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
            _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");

            return RedirectToAction("ShippedConsignments");
        }
    }
}