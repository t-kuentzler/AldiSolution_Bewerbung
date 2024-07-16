using Microsoft.AspNetCore.Mvc;

namespace DpdPushTrackingApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TrackingPushController : ControllerBase
{
    private readonly ITrackingDataService _trackingDataService;
    private readonly ILogger<TrackingPushController> _logger;


    public TrackingPushController(ITrackingDataService trackingDataService,
        ILogger<TrackingPushController> logger)
    {
        _trackingDataService = trackingDataService;
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
                
            await _trackingDataService.ProcessTrackingData(trackingData);
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