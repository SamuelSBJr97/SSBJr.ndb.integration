using Microsoft.AspNetCore.Mvc;
using SSBJr.ndb.integration.Web.ApiService.Services;
using SSBJr.ndb.integration.Web.ApiService.Models;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

/// <summary>
/// Controller para compatibilidade com o Blazor
/// Adapta os dados do ApiManagerService para o formato esperado pelo Blazor
/// </summary>
[ApiController]
[Route("api/blazor")]
[Produces("application/json")]
public class BlazorCompatibilityController : ControllerBase
{
    private readonly IApiManagerService _apiManagerService;
    private readonly ILogger<BlazorCompatibilityController> _logger;

    public BlazorCompatibilityController(IApiManagerService apiManagerService, ILogger<BlazorCompatibilityController> logger)
    {
        _apiManagerService = apiManagerService;
        _logger = logger;
    }

    /// <summary>
    /// Criar interface de API compatível com Blazor
    /// </summary>
    [HttpPost("interfaces")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CreateInterface([FromBody] JsonElement request)
    {
        try
        {
            // Extract data from Blazor request format
            var deploymentRequest = new ApiDeploymentRequest
            {
                Name = GetStringProperty(request, "name") ?? "",
                Description = GetStringProperty(request, "description") ?? "",
                SwaggerJson = GetStringProperty(request, "swaggerJson") ?? "",
                Configuration = ExtractConfiguration(request)
            };

            if (string.IsNullOrWhiteSpace(deploymentRequest.Name))
            {
                return BadRequest(new { Error = "Nome é obrigatório" });
            }

            var api = await _apiManagerService.CreateApiAsync(deploymentRequest);
            
            var result = ConvertToBlazorFormat(api);
            return CreatedAtAction(nameof(GetInterface), new { id = api.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating interface via Blazor compatibility layer");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor" });
        }
    }

    /// <summary>
    /// Obter interface por ID em formato compatível com Blazor
    /// </summary>
    [HttpGet("interfaces/{id:guid}")]
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

            var result = ConvertToBlazorFormat(api);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting interface {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = "Erro interno do servidor" });
        }
    }

    private object ConvertToBlazorFormat(ApiDefinition api)
    {
        return new
        {
            id = api.Id,
            name = api.Name,
            description = api.Description,
            type = "REST",
            version = "1.0.0",
            graphQLSchema = "",
            swaggerJson = api.SwaggerJson,
            infrastructure = new
            {
                database = new { type = "PostgreSQL" },
                cache = new { type = "Redis" },
                messaging = new { type = "None" },
                storage = new { type = "None" },
                scaling = new { minInstances = 1, maxInstances = 10 }
            },
            security = new
            {
                authentication = new { type = "JWT" },
                authorization = new { },
                encryption = new { enableTLS = true },
                rateLimit = new { enabled = true, requestsPerMinute = 100 },
                cors = new { enabled = true }
            },
            monitoring = new
            {
                logging = new { level = "Information" },
                metrics = new { enabled = true },
                tracing = new { enabled = true }
            },
            status = api.Status.ToString(),
            createdAt = api.CreatedAt,
            updatedAt = (DateTime?)null,
            createdBy = "system",
            updatedBy = (string?)null,
            tags = new List<string>(),
            metadata = api.Metadata,
            errorMessage = api.ErrorMessage,
            deploymentInfo = (object?)null,
            baseUrl = api.BaseUrl,
            lastHealthCheck = api.LastHealthCheck
        };
    }

    private string? GetStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
    }

    private Dictionary<string, object> ExtractConfiguration(JsonElement request)
    {
        var config = new Dictionary<string, object>();
        
        if (request.TryGetProperty("infrastructure", out var infra))
        {
            config["infrastructure"] = JsonSerializer.Serialize(infra);
        }
        
        if (request.TryGetProperty("security", out var security))
        {
            config["security"] = JsonSerializer.Serialize(security);
        }
        
        if (request.TryGetProperty("monitoring", out var monitoring))
        {
            config["monitoring"] = JsonSerializer.Serialize(monitoring);
        }

        return config;
    }
}