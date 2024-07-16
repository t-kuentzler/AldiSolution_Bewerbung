using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shared.Contracts;
using Shared.Models;

namespace DpdPushTrackingApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrackingPushController : ControllerBase
{
    private readonly IDpdTrackingDataService _dpdTrackingDataService;
    private readonly ILogger<TrackingPushController> _logger;


    public TrackingPushController(IDpdTrackingDataService dpdTrackingDataService,
        ILogger<TrackingPushController> logger)
    {
        _dpdTrackingDataService = dpdTrackingDataService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> Index([FromQuery] TrackingData trackingData)
    {
        if (string.IsNullOrWhiteSpace(trackingData.pushid))
        {
            _logger.LogWarning("Anfrage ohne pushId erhalten.");
            return BadRequest("Es fehlt die erforderliche pushId.");
        }
            
        try
        {
            string trackingDataJson = JsonConvert.SerializeObject(trackingData);
            _logger.LogInformation($"Empfangene Tracking-Daten: {trackingDataJson}");
                
            await _dpdTrackingDataService.ProcessTrackingData(trackingData);
            var responseXml = $"<push><pushid>{trackingData.pushid}</pushid><status>OK</status></push>";
            return Ok(responseXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Verarbeitung der Tracking-Daten.");
            return StatusCode(500, "Ein interner Fehler ist aufgetreten."); 
        }
    }

}