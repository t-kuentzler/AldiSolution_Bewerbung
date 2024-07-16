using AldiOrderManagement.Services;
using Microsoft.AspNetCore.Mvc;

namespace AldiOrderManagement.Controllers;

public class StatisticController : Controller
{
    private readonly IStatisticService _statisticService;

    public StatisticController(IStatisticService statisticService)
    {
        _statisticService = statisticService;
    }
    
    public async Task<IActionResult> Index()
    {
        var productSales = await _statisticService.GetProductSalesAsync();
        return View(productSales);
    }
}