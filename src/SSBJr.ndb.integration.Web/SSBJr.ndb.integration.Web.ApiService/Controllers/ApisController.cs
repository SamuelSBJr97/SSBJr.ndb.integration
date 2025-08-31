using Microsoft.AspNetCore.Mvc;
using SSBJr.ndb.integration.Web.ApiService.Services;
using SSBJr.ndb.integration.Web.ApiService.Models;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ApisController : ControllerBase
{
    private readonly IApiManagerService _apiManagerService;
    private readonly ILogger<ApisController> _logger;

    public ApisController(IApiManagerService apiManagerService, ILogger<ApisController> logger)
    {
        _apiManagerService = apiManagerService;
        _logger = logger;
    }

    /// <summary>
    /// Criar nova API a partir de arquivo Swagger
    /// </summary>
    /// <param name="request">Dados da API a ser criada</param>
    /// <returns>API criada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiDefinition), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiDefinition>> CreateApi([FromBody] ApiDeploymentRequest request)
    {
        try
        {
            var api = await _apiManagerService.CreateApiAsync(request);
            return CreatedAtAction(nameof(GetApi), new { id = api.Id }, api);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API {Name}", request.Name);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao criar API" });
        }
    }

    /// <summary>
    /// Obter todas as APIs
    /// </summary>
    /// <returns>Lista de todas as APIs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApiDefinition>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ApiDefinition>>> GetAllApis()
    {
        try
        {
            var apis = await _apiManagerService.GetAllApisAsync();
            return Ok(apis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all APIs");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter APIs" });
        }
    }

    /// <summary>
    /// Obter API por ID
    /// </summary>
    /// <param name="id">ID da API</param>
    /// <returns>Dados da API</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiDefinition), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiDefinition>> GetApi(Guid id)
    {
        try
        {
            var api = await _apiManagerService.GetApiAsync(id);
            if (api == null)
            {
                return NotFound(new { Error = "API não encontrada" });
            }

            return Ok(api);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter API" });
        }
    }

    /// <summary>
    /// Deletar API
    /// </summary>
    /// <param name="id">ID da API</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteApi(Guid id)
    {
        try
        {
            var success = await _apiManagerService.DeleteApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "API não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao deletar API" });
        }
    }

    /// <summary>
    /// Iniciar API
    /// </summary>
    /// <param name="id">ID da API</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StartApi(Guid id)
    {
        try
        {
            var success = await _apiManagerService.StartApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "API não encontrada" });
            }

            return Ok(new { Message = "API iniciada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting API {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao iniciar API" });
        }
    }

    /// <summary>
    /// Parar API
    /// </summary>
    /// <param name="id">ID da API</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StopApi(Guid id)
    {
        try
        {
            var success = await _apiManagerService.StopApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "API não encontrada" });
            }

            return Ok(new { Message = "API parada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao parar API" });
        }
    }

    /// <summary>
    /// Verificar saúde da API
    /// </summary>
    /// <param name="id">ID da API</param>
    /// <returns>Status de saúde da API</returns>
    [HttpGet("{id:guid}/health")]
    [ProducesResponseType(typeof(ApiHealthCheck), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiHealthCheck>> CheckApiHealth(Guid id)
    {
        try
        {
            var healthCheck = await _apiManagerService.CheckApiHealthAsync(id);
            return Ok(healthCheck);
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "API não encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for API {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao verificar saúde" });
        }
    }
}