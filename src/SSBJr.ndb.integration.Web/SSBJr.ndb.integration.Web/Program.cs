using SSBJr.ndb.integration.Web;
using SSBJr.ndb.integration.Web.Components;
using SSBJr.ndb.integration.Web.Services;
using SSBJr.ndb.integration.Web.Models;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Conditionally add Redis only if connection string is configured
var redisConnectionString = builder.Configuration.GetConnectionString("cache");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    builder.AddRedisOutputCache("cache");
}
else
{
    // Use in-memory caching as fallback
    builder.Services.AddOutputCache();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add HTTP Client
builder.Services.AddHttpClient();

// Add SignalR for real-time notifications
builder.Services.AddSignalR();

// Add custom services
builder.Services.AddScoped<IApiInterfaceService, ApiInterfaceService>();
builder.Services.AddScoped<IDockerContainerService, DockerContainerService>();
builder.Services.AddScoped<IInfrastructureService, InfrastructureService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Add background services
builder.Services.AddHostedService<AuditArchiveService>();

// Add CORS for API access
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "https://localhost:7080")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseOutputCache();
app.UseCors("ApiPolicy");

// Map SignalR Hub for notifications
app.MapHub<NotificationHub>("/notifications");

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add API endpoints for CRUD operations
var apiGroup = app.MapGroup("/api/interfaces")
    .WithTags("API Interfaces")
    .RequireCors("ApiPolicy");

apiGroup.MapGet("/", async (IApiInterfaceService service, int page = 1, int pageSize = 10, string? search = null) =>
{
    var interfaces = await service.GetAllAsync(page, pageSize, search);
    return Results.Ok(interfaces);
});

apiGroup.MapGet("/{id:guid}", async (Guid id, IApiInterfaceService service) =>
{
    var apiInterface = await service.GetByIdAsync(id);
    return apiInterface != null ? Results.Ok(apiInterface) : Results.NotFound();
});

apiGroup.MapPost("/", async (ApiInterfaceCreateRequest request, IApiInterfaceService service) =>
{
    try
    {
        var userId = "demo-user";
        var apiInterface = await service.CreateAsync(request, userId);
        return Results.Created($"/api/interfaces/{apiInterface.Id}", apiInterface);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

apiGroup.MapPut("/{id:guid}", async (Guid id, ApiInterfaceUpdateRequest request, IApiInterfaceService service) =>
{
    try
    {
        var userId = "demo-user";
        var apiInterface = await service.UpdateAsync(id, request, userId);
        return Results.Ok(apiInterface);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

apiGroup.MapDelete("/{id:guid}", async (Guid id, IApiInterfaceService service) =>
{
    var success = await service.DeleteAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
});

// Deployment and management endpoints
apiGroup.MapPost("/{id:guid}/deploy", async (Guid id, IApiInterfaceService service) =>
{
    var success = await service.DeployAsync(id);
    return success ? Results.Ok(new { Message = "Deployment started" }) : Results.NotFound();
});

apiGroup.MapPost("/{id:guid}/stop", async (Guid id, IApiInterfaceService service) =>
{
    var success = await service.StopAsync(id);
    return success ? Results.Ok(new { Message = "API stopped" }) : Results.NotFound();
});

apiGroup.MapPost("/{id:guid}/restart", async (Guid id, IApiInterfaceService service) =>
{
    var success = await service.RestartAsync(id);
    return success ? Results.Ok(new { Message = "API restarted" }) : Results.NotFound();
});

apiGroup.MapPost("/{id:guid}/validate", async (Guid id, IApiInterfaceService service) =>
{
    try
    {
        var apiInterface = await service.ValidateAsync(id);
        return Results.Ok(apiInterface);
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

// Monitoring endpoints
apiGroup.MapGet("/{id:guid}/metrics", async (Guid id, IApiInterfaceService service) =>
{
    var metrics = await service.GetMetricsAsync(id);
    return Results.Ok(metrics);
});

apiGroup.MapGet("/{id:guid}/logs", async (Guid id, IApiInterfaceService service, int lines = 100) =>
{
    var logs = await service.GetLogsAsync(id, lines);
    return Results.Ok(logs);
});

// Health Check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    Status = "Healthy", 
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName
}));

// Audit endpoints
var auditGroup = app.MapGroup("/api/audit")
    .WithTags("Audit")
    .RequireCors("ApiPolicy");

auditGroup.MapGet("/logs", async (IAuditService service, int page = 1, int pageSize = 50, string? entityId = null, string? userId = null, DateTime? fromDate = null, DateTime? toDate = null) =>
{
    var logs = await service.GetLogsAsync(page, pageSize, entityId, userId, fromDate, toDate);
    return Results.Ok(logs);
});

auditGroup.MapGet("/statistics", async (IAuditService service) =>
{
    var stats = await service.GetAuditStatisticsAsync();
    return Results.Ok(stats);
});

app.MapDefaultEndpoints();

app.Run();

// Simple NotificationHub for SignalR
public class NotificationHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
