using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants;
using Shared.Contracts;
using Shared.Entities;
using Shared.Exceptions;
using Shared.Models;

namespace AldiOrderManagement.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IFileService _fileService;
        private readonly ILogger<OrderController> _logger;


        public OrderController(IOrderService orderService, IFileService fileService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _fileService = fileService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            string status = SharedStatus.InProgress;
            try
            {
                var orders = await _orderService.GetAllOrdersByStatusAsync(status);

                return View(orders.OrderByDescending(o => o.Created).ToList());
            }
            catch (OrderServiceException ex)
            {
                TempData["ErrorMessage"] =
                    $"Fehler beim Abrufen von Bestellungen mit dem Status '{status}': {ex.Message}";
                return View(new List<Order>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
                _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
                return View(new List<Order>());
            }
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);

                return View(order);
            }
            catch (OrderServiceException ex)
            {
                TempData["ErrorMessage"] = $"Fehler beim Abrufen von Bestellungsdetails: {ex.Message}";
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
                _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
                return View();
            }
        }

        public async Task<IActionResult> CancelledOrders(SearchTerm searchTerm)
        {
            string status = SharedStatus.Canceled;

            try
            {
                IEnumerable<Order> orders;

                if (!string.IsNullOrWhiteSpace(searchTerm.value))
                {
                    // Suchlogik
                    orders = await _orderService.SearchOrdersAsync(searchTerm, status);
                }
                else
                {
                    orders = await _orderService.GetAllOrdersByStatusAsync(status);
                }

                ViewBag.SearchTerm = searchTerm.value;

                var orderList = orders.OrderByDescending(o => o.Created).ToList();
                return View(orderList);
            }
            catch (ValidationException ex)
            {
                var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
                TempData["ErrorMessage"] = errorMessages;
                return View(new List<Order>());
            }
            catch (OrderServiceException ex)
            {
                TempData["ErrorMessage"] = "Fehler beim Laden der stornierten Bestellungen: " + ex.Message;
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Ein unerwarteter Fehler ist aufgetreten.";
                _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
                return View();
            }
        }

        public async Task<IActionResult> DeliveredOrders(SearchTerm searchTerm, bool all = false)
        {
            string status = SharedStatus.Delivered;

            try
            {
                IEnumerable<Order> orders;

                if (!string.IsNullOrWhiteSpace(searchTerm.value))
                {
                    orders = await _orderService.SearchOrdersAsync(searchTerm, status);
                }
                else
                {
                    orders = await _orderService.GetAllOrdersByStatusAsync(status);
                }

                var totalOrdersCount = orders.Count();  // Gesamtanzahl der Bestellungen ermitteln
                ViewBag.TotalOrdersCount = totalOrdersCount;

                ViewBag.SearchTerm = searchTerm.value;

                var orderList = orders.OrderByDescending(o => o.Created).ToList();
        
                if (!all)
                {
                    orderList = orderList.Take(20).ToList();
                }

                return View(orderList);
            }
            catch (ValidationException ex)
            {
                var errorMessages = string.Join("\n", ex.Errors.Select(error => error.ErrorMessage));
                TempData["ErrorMessage"] = errorMessages;
                return View(new List<Order>());
            }
            catch (OrderServiceException ex)
            {
                TempData["ErrorMessage"] = "Fehler beim Laden der gelieferten Bestellungen: " + ex.Message;
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
        public async Task<IActionResult> CancelOrderEntries(int orderId, string orderCode,
            Dictionary<int, CancelOrderEntryModel> cancelledEntries)
        {
            try
            {
                bool cancellationResult =
                    await _orderService.ProcessOrderEntriesCancellationAsync(orderId, orderCode, cancelledEntries);

                if (cancellationResult)
                {
                    TempData["SuccessMessage"] = "Die Stornierung wurde erfolgreich durchgeführt.";
                    return RedirectToAction("OrderDetails", new { id = orderId });
                }

                TempData["ErrorMessage"] = "Es ist ein Fehler bei dem Aufruf der API aufgetreten.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (ValidationException ex)
            {
                TempData["ErrorMessage"] = ex.Errors.FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Es ist ein Fehler beim Stornieren der Bestellpositionen aufgetreten.";
                _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");
                return RedirectToAction("OrderDetails", new { id = orderId });
            }
        }

        public async Task<IActionResult> OrderDetailsWithReturn(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ExportToExcel(List<int> selectedOrders)
        {
            try
            {
                if (selectedOrders.Count == 0)
                {
                    TempData["ErrorMessage"] = "Bitte markieren Sie mindestens eine Bestellung zum Exportieren.";
                    return RedirectToAction("Index");
                }

                var orders = await _orderService.GetOrdersByIds(selectedOrders);
                var content = _fileService.CreateExcelFileInProgressOrders(orders);
                await _orderService.UpdateOrderExportedValue(orders, true);

                // Datei auf dem Server speichern und Pfad oder Bezeichner in TempData speichern
                var fileId = _fileService.SaveFileOnServer(content); // Implementieren Sie diese Methode entsprechend

                return RedirectToAction("DownloadConfirmation", new { fileId = fileId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Es ist ein Fehler beim Exportieren der Daten aufgetreten.";
                _logger.LogError(ex, "Es ist ein unerwarteter Fehler aufgetreten.");

                return RedirectToAction("Index");
            }
        }

        public IActionResult DownloadConfirmation(string fileId)
        {
            ViewBag.FileId = fileId;
            return View();
        }

        public async Task<IActionResult> DownloadFile(string fileId)
        {
            var filePath = _fileService.GetFilePathByFileId(fileId);
            if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }

            memoryStream.Position = 0;


            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Path.GetFileName(filePath));
        }
    }
}