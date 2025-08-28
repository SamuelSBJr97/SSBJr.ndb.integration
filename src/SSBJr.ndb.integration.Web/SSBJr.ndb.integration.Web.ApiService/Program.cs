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
        Title = "SSBJr.ndb.Integration API",
        Version = "v1",
        Description = "API para provisionamento unificado de bancos de dados"
    });
});

// Add CORS for React app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable Swagger in all environments for this integration platform
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SSBJr.ndb.Integration API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowReactApp");

// Add sample endpoints for database provisioning
app.MapGet("/api/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .WithName("GetHealth")
    .WithOpenApi();

app.MapGet("/api/providers", () => new {
    Providers = new[] { "AWS", "Azure" },
    DatabaseTypes = new[] { "SQL", "NoSQL" }
})
    .WithName("GetProviders")
    .WithOpenApi();

app.MapPost("/api/databases", (DatabaseRequest request) =>
{
    // Simulate database provisioning
    return Results.Ok(new {
        Id = Guid.NewGuid(),
        Status = "Provisioning",
        Provider = request.Provider,
        DatabaseType = request.DatabaseType,
        Name = request.Name,
        Region = request.Region
    });
})
    .WithName("ProvisionDatabase")
    .WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

// DTOs for API
public record DatabaseRequest(string Provider, string DatabaseType, string Name, string Region);