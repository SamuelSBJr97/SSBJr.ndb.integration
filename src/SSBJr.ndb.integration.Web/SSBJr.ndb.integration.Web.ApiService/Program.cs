using SSBJr.ndb.integration.Web.ApiService.Models;
using SSBJr.ndb.integration.Web.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "SSBJr.ndb.Integration API Manager",
        Version = "v1",
        Description = "API para gerenciamento e hospedagem de APIs a partir de arquivos Swagger"
    });
});

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Add HTTP Client
builder.Services.AddHttpClient();

// Add custom services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IApiManagerService, ApiManagerService>();

// Add CORS for all frontends
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", // React
                "http://localhost:5173", // Vite React
                "https://localhost:7080", // Blazor Web
                "https://localhost:7081", // Blazor WebAssembly
                "http://localhost:8080"   // API
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SSBJr.ndb.Integration API Manager v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");

// Map SignalR Hub
app.MapHub<NotificationHub>("/notifications");

// Health check endpoint
app.MapGet("/api/health", () => new {
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "API Manager"
})
.WithName("GetHealth")
.WithOpenApi();

// API Management endpoints
var apiGroup = app.MapGroup("/api/apis")
    .WithTags("API Management")
    .WithOpenApi();

apiGroup.MapPost("/", async (ApiDeploymentRequest request, IApiManagerService apiManager) =>
{
    try
    {
        var api = await apiManager.CreateApiAsync(request);
        return Results.Created($"/api/apis/{api.Id}", api);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
})
.WithName("CreateApi")
.WithSummary("Criar nova API a partir de arquivo Swagger");

apiGroup.MapGet("/", async (IApiManagerService apiManager) =>
{
    var apis = await apiManager.GetAllApisAsync();
    return Results.Ok(apis);
})
.WithName("GetAllApis")
.WithSummary("Obter todas as APIs");

apiGroup.MapGet("/{id:guid}", async (Guid id, IApiManagerService apiManager) =>
{
    var api = await apiManager.GetApiAsync(id);
    return api != null ? Results.Ok(api) : Results.NotFound();
})
.WithName("GetApi")
.WithSummary("Obter API por ID");

apiGroup.MapDelete("/{id:guid}", async (Guid id, IApiManagerService apiManager) =>
{
    var success = await apiManager.DeleteApiAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteApi")
.WithSummary("Deletar API");

apiGroup.MapPost("/{id:guid}/start", async (Guid id, IApiManagerService apiManager) =>
{
    var success = await apiManager.StartApiAsync(id);
    return success ? Results.Ok(new { Message = "API iniciada" }) : Results.NotFound();
})
.WithName("StartApi")
.WithSummary("Iniciar API");

apiGroup.MapPost("/{id:guid}/stop", async (Guid id, IApiManagerService apiManager) =>
{
    var success = await apiManager.StopApiAsync(id);
    return success ? Results.Ok(new { Message = "API parada" }) : Results.NotFound();
})
.WithName("StopApi")
.WithSummary("Parar API");

apiGroup.MapGet("/{id:guid}/health", async (Guid id, IApiManagerService apiManager) =>
{
    try
    {
        var healthCheck = await apiManager.CheckApiHealthAsync(id);
        return Results.Ok(healthCheck);
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
})
.WithName("CheckApiHealth")
.WithSummary("Verificar saúde da API");

// File upload endpoint for Swagger files
app.MapPost("/api/swagger/upload", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("Arquivo não fornecido");

    if (!file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest("Apenas arquivos JSON são aceitos");

    try
    {
        using var stream = new StreamReader(file.OpenReadStream());
        var content = await stream.ReadToEndAsync();

        // Validar JSON
        System.Text.Json.JsonDocument.Parse(content);

        return Results.Ok(new { Content = content, FileName = file.FileName });
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest("Arquivo JSON inválido");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Erro ao processar arquivo: {ex.Message}");
    }
})
.WithName("UploadSwagger")
.WithSummary("Upload de arquivo Swagger JSON")
.DisableAntiforgery();

app.MapDefaultEndpoints();

app.Run();

// DTOs for API
public record DatabaseRequest(string Provider, string DatabaseType, string Name, string Region);