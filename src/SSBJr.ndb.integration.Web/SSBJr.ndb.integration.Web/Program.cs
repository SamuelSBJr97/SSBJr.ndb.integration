using SSBJr.ndb.integration.Web;
using SSBJr.ndb.integration.Web.Components;
using SSBJr.ndb.integration.Web.Services;
using SSBJr.ndb.integration.Web.Models;
using SSBJr.ndb.integration.Web.Data;
using SSBJr.ndb.integration.Web.Middleware;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// Add Entity Framework with fallback
var postgresConnectionString = builder.Configuration.GetConnectionString("postgres");
if (!string.IsNullOrEmpty(postgresConnectionString))
{
    builder.Services.AddDbContext<ApiManagerDbContext>(options =>
        options.UseNpgsql(postgresConnectionString));
}
else
{
    builder.Services.AddDbContext<ApiManagerDbContext>(options =>
        options.UseInMemoryDatabase("ApiManagerDb"));
}

// Add Authentication & Authorization
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? "your-super-secret-key-for-jwt-tokens-at-least-256-bits-long";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.GetValue<string>("Issuer") ?? "SSBJr.ApiManager",
            ValidAudience = jwtSettings.GetValue<string>("Audience") ?? "SSBJr.ApiManager.Users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notifications"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

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
builder.Services.AddScoped<IAuthService, AuthService>();

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

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiManagerDbContext>();
    try
    {
        await context.Database.EnsureCreatedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not initialize database, continuing with in-memory fallback");
    }
}

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

// Add authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Add custom authorization middleware for fine-grained permissions
app.UseMiddleware<AuthorizationMiddleware>();

// Map SignalR Hub for notifications
app.MapHub<NotificationHub>("/notifications");

// Map Razor components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Authentication endpoints
var authGroup = app.MapGroup("/api/auth")
    .WithTags("Authentication")
    .RequireCors("ApiPolicy");

authGroup.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("Login")
.WithSummary("User login")
.AllowAnonymous();

authGroup.MapPost("/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return result.Success ? Results.Created("/api/auth/me", result) : Results.BadRequest(result);
})
.WithName("Register")
.WithSummary("User registration")
.AllowAnonymous();

authGroup.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
{
    var result = await authService.RefreshTokenAsync(request.RefreshToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithName("RefreshToken")
.WithSummary("Refresh access token")
.AllowAnonymous();

authGroup.MapPost("/logout", async (RefreshTokenRequest request, IAuthService authService) =>
{
    await authService.LogoutAsync(request.RefreshToken);
    return Results.Ok(new { Message = "Logged out successfully" });
})
.WithName("Logout")
.WithSummary("User logout")
.RequireAuthorization();

authGroup.MapGet("/me", async (HttpContext context, IAuthService authService) =>
{
    var user = await authService.GetCurrentUserAsync(context.User);
    return user != null ? Results.Ok(new UserDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        Permissions = user.Permissions
    }) : Results.NotFound();
})
.WithName("GetCurrentUser")
.WithSummary("Get current user info")
.RequireAuthorization();

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

apiGroup.MapPost("/", async (ApiInterfaceCreateRequest request, IApiInterfaceService service, HttpContext context) =>
{
    try
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "demo-user";
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

apiGroup.MapPut("/{id:guid}", async (Guid id, ApiInterfaceUpdateRequest request, IApiInterfaceService service, HttpContext context) =>
{
    try
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "demo-user";
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

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
