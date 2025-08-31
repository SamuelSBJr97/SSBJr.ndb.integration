using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SSBJr.ndb.integration.Web.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SwaggerController : ControllerBase
{
    private readonly ILogger<SwaggerController> _logger;

    public SwaggerController(ILogger<SwaggerController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Upload de arquivo Swagger JSON
    /// </summary>
    /// <param name="file">Arquivo Swagger JSON</param>
    /// <returns>Conteúdo do arquivo validado</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<ActionResult> UploadSwagger(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "Arquivo não fornecido" });
        }

        if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { Error = "Apenas arquivos JSON são aceitos" });
        }

        try
        {
            using var stream = new StreamReader(file.OpenReadStream());
            var content = await stream.ReadToEndAsync();

            // Validar JSON
            var document = JsonDocument.Parse(content);
            
            // Verificar se é um documento Swagger/OpenAPI válido
            if (!document.RootElement.TryGetProperty("openapi", out _) && 
                !document.RootElement.TryGetProperty("swagger", out _))
            {
                return BadRequest(new { Error = "Arquivo não é um documento Swagger/OpenAPI válido" });
            }

            return Ok(new { Content = content, FileName = file.FileName });
        }
        catch (JsonException)
        {
            return BadRequest(new { Error = "Arquivo JSON inválido" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Swagger file");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { Error = $"Erro ao processar arquivo: {ex.Message}" });
        }
    }
}