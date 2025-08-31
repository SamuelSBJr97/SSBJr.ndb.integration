using Microsoft.AspNetCore.Mvc;
using SSBJr.ndb.integration.Web.ApiService.Services;
using SSBJr.ndb.integration.Web.ApiService.Models;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ServicesController : ControllerBase
{
    private readonly IApiManagerService _apiManagerService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IApiManagerService apiManagerService, ILogger<ServicesController> logger)
    {
        _apiManagerService = apiManagerService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todos os serviços disponíveis
    /// </summary>
    /// <returns>Lista de todos os serviços</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiDefinition>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ApiDefinition>>> GetAllServices()
    {
        try
        {
            var services = await _apiManagerService.GetAllApisAsync();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter serviços" });
        }
    }

    /// <summary>
    /// Obter serviço por ID
    /// </summary>
    /// <param name="id">ID do serviço</param>
    /// <returns>Dados do serviço</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiDefinition>> GetService(Guid id)
    {
        try
        {
            var service = await _apiManagerService.GetApiAsync(id);
            if (service == null)
            {
                return NotFound(new { Error = "Serviço não encontrado" });
            }

            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter serviço" });
        }
    }

    /// <summary>
    /// Verificar saúde de um serviço
    /// </summary>
    /// <param name="id">ID do serviço</param>
    /// <returns>Status de saúde do serviço</returns>
    [HttpGet("{id:guid}/health")]
    [ProducesResponseType(typeof(ApiHealthCheck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiHealthCheck>> CheckServiceHealth(Guid id)
    {
        try
        {
            var healthCheck = await _apiManagerService.CheckApiHealthAsync(id);
            return Ok(healthCheck);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "Serviço não encontrado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for service {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao verificar saúde" });
        }
    }
}