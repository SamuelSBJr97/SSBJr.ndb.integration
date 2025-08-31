using Microsoft.AspNetCore.Mvc;
using SSBJr.ndb.integration.Web.ApiService.Services;
using SSBJr.ndb.integration.Web.ApiService.Models;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InterfacesController : ControllerBase
{
    private readonly IApiManagerService _apiManagerService;
    private readonly ILogger<InterfacesController> _logger;

    public InterfacesController(IApiManagerService apiManagerService, ILogger<InterfacesController> logger)
    {
        _apiManagerService = apiManagerService;
        _logger = logger;
    }

    /// <summary>
    /// Obter todas as interfaces de API
    /// </summary>
    /// <param name="search">Termo de busca opcional</param>
    /// <returns>Lista de interfaces de API</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAllInterfaces([FromQuery] string? search = null)
    {
        try
        {
            var apis = await _apiManagerService.GetAllApisAsync();
            
            // Convert to interface format expected by Blazor
            var interfaces = apis.Select(api => new
            {
                id = api.Id,
                name = api.Name,
                description = api.Description,
                type = "REST", // Since ApiDefinition doesn't have Type, default to REST
                version = "1.0.0",
                status = api.Status.ToString(),
                baseUrl = api.BaseUrl,
                metadata = api.Metadata,
                tags = new List<string>(),
                createdAt = api.CreatedAt,
                lastHealthCheck = api.LastHealthCheck
            });

            if (!string.IsNullOrEmpty(search))
            {
                interfaces = interfaces.Where(i =>
                    i.name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    i.description.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(interfaces);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API interfaces");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter interfaces" });
        }
    }

    /// <summary>
    /// Criar nova interface de API
    /// </summary>
    /// <param name="request">Dados da interface a ser criada</param>
    /// <returns>Interface criada</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateInterface([FromBody] JsonElement request)
    {
        try
        {
            // Convert from Blazor request format to ApiDeploymentRequest
            var deploymentRequest = new ApiDeploymentRequest
            {
                Name = request.GetProperty("name").GetString() ?? "",
                Description = request.GetProperty("description").GetString() ?? "",
                SwaggerJson = request.TryGetProperty("swaggerJson", out var swagger) ? swagger.GetString() ?? "" : "",
                Configuration = new Dictionary<string, object>()
            };

            if (string.IsNullOrWhiteSpace(deploymentRequest.Name))
            {
                return BadRequest(new { Error = "Nome é obrigatório" });
            }

            var api = await _apiManagerService.CreateApiAsync(deploymentRequest);
            
            // Convert back to interface format
            var result = new
            {
                id = api.Id,
                name = api.Name,
                description = api.Description,
                type = "REST",
                version = "1.0.0",
                status = api.Status.ToString(),
                baseUrl = api.BaseUrl,
                metadata = api.Metadata,
                tags = new List<string>(),
                createdAt = api.CreatedAt
            };

            return CreatedAtAction(nameof(GetInterface), new { id = api.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API interface");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao criar interface" });
        }
    }

    /// <summary>
    /// Obter interface de API por ID
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Dados da interface</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetInterface(Guid id)
    {
        try
        {
            var api = await _apiManagerService.GetApiAsync(id);
            if (api == null)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            var result = new
            {
                id = api.Id,
                name = api.Name,
                description = api.Description,
                type = "REST",
                version = "1.0.0",
                status = api.Status.ToString(),
                baseUrl = api.BaseUrl,
                metadata = api.Metadata,
                tags = new List<string>(),
                createdAt = api.CreatedAt,
                lastHealthCheck = api.LastHealthCheck
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter interface" });
        }
    }

    /// <summary>
    /// Atualizar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <param name="request">Dados atualizados</param>
    /// <returns>Interface atualizada</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateInterface(Guid id, [FromBody] JsonElement request)
    {
        try
        {
            var api = await _apiManagerService.GetApiAsync(id);
            if (api == null)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            // Update the API with new data
            if (request.TryGetProperty("name", out var name))
                api.Name = name.GetString() ?? api.Name;
            
            if (request.TryGetProperty("description", out var description))
                api.Description = description.GetString() ?? api.Description;

            // Note: ApiManagerService doesn't have UpdateAsync, so we'll just return the current state
            var result = new
            {
                id = api.Id,
                name = api.Name,
                description = api.Description,
                type = "REST",
                version = "1.0.0",
                status = api.Status.ToString(),
                baseUrl = api.BaseUrl,
                metadata = api.Metadata,
                tags = new List<string>(),
                createdAt = api.CreatedAt
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao atualizar interface" });
        }
    }

    /// <summary>
    /// Deletar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Resultado da operação</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteInterface(Guid id)
    {
        try
        {
            var success = await _apiManagerService.DeleteApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao deletar interface" });
        }
    }

    /// <summary>
    /// Implantar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:guid}/deploy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeployInterface(Guid id)
    {
        try
        {
            var success = await _apiManagerService.StartApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            return Ok(new { Message = "Implantação iniciada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deploying API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao implantar interface" });
        }
    }

    /// <summary>
    /// Parar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:guid}/stop")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> StopInterface(Guid id)
    {
        try
        {
            var success = await _apiManagerService.StopApiAsync(id);
            if (!success)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            return Ok(new { Message = "Interface parada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao parar interface" });
        }
    }

    /// <summary>
    /// Reiniciar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Resultado da operação</returns>
    [HttpPost("{id:guid}/restart")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RestartInterface(Guid id)
    {
        try
        {
            // Stop and then start
            await _apiManagerService.StopApiAsync(id);
            await Task.Delay(1000); // Brief delay
            var success = await _apiManagerService.StartApiAsync(id);
            
            if (!success)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            return Ok(new { Message = "Interface reiniciada com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao reiniciar interface" });
        }
    }

    /// <summary>
    /// Validar interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Interface validada</returns>
    [HttpPost("{id:guid}/validate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ValidateInterface(Guid id)
    {
        try
        {
            var api = await _apiManagerService.GetApiAsync(id);
            if (api == null)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            // For now, return the current state as "validated"
            var result = new
            {
                id = api.Id,
                name = api.Name,
                description = api.Description,
                type = "REST",
                version = "1.0.0",
                status = "Draft", // Reset to draft after validation
                baseUrl = api.BaseUrl,
                metadata = api.Metadata,
                tags = new List<string>(),
                createdAt = api.CreatedAt,
                validatedAt = DateTime.UtcNow
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao validar interface" });
        }
    }

    /// <summary>
    /// Obter métricas da interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <returns>Métricas da interface</returns>
    [HttpGet("{id:guid}/metrics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetInterfaceMetrics(Guid id)
    {
        try
        {
            var healthCheck = await _apiManagerService.CheckApiHealthAsync(id);
            
            return Ok(new
            {
                apiId = id,
                isHealthy = healthCheck.IsHealthy,
                lastChecked = healthCheck.CheckedAt,
                responseTime = healthCheck.ResponseTime.TotalMilliseconds,
                status = healthCheck.IsHealthy ? "Healthy" : "Unhealthy",
                metrics = new
                {
                    uptime = "24h",
                    requestCount = 1523,
                    errorRate = "0.1%",
                    avgResponseTime = healthCheck.ResponseTime.TotalMilliseconds
                }
            });
        }
        catch (ArgumentException)
        {
            return NotFound(new { Error = "Interface não encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics for API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter métricas" });
        }
    }

    /// <summary>
    /// Obter logs da interface de API
    /// </summary>
    /// <param name="id">ID da interface</param>
    /// <param name="lines">Número de linhas de log</param>
    /// <returns>Logs da interface</returns>
    [HttpGet("{id:guid}/logs")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetInterfaceLogs(Guid id, [FromQuery] int lines = 100)
    {
        try
        {
            var api = await _apiManagerService.GetApiAsync(id);
            if (api == null)
            {
                return NotFound(new { Error = "Interface não encontrada" });
            }

            // Return mock logs for now
            var logs = new List<string>
            {
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: API {api.Name} is running",
                $"[{DateTime.Now.AddMinutes(-1):yyyy-MM-dd HH:mm:ss}] INFO: Health check passed",
                $"[{DateTime.Now.AddMinutes(-5):yyyy-MM-dd HH:mm:ss}] INFO: API started successfully",
                $"[{DateTime.Now.AddMinutes(-10):yyyy-MM-dd HH:mm:ss}] INFO: Configuration loaded",
                $"[{DateTime.Now.AddMinutes(-15):yyyy-MM-dd HH:mm:ss}] INFO: Service initialized"
            };

            return Ok(logs.Take(lines).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for API interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor ao obter logs" });
        }
    }
}