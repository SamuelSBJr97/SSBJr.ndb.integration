using Microsoft.AspNetCore.Mvc;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check do serviço
    /// </summary>
    /// <returns>Status de saúde do serviço</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        try
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Service = "API Manager",
                Version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in health check");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Service = "API Manager",
                Error = ex.Message
            });
        }
    }
}