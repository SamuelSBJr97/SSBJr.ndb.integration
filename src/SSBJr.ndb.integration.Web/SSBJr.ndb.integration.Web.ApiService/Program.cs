using SSBJr.ndb.integration.Web.ApiService.Models;
using SSBJr.ndb.integration.Web.ApiService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

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
    
    // Include XML comments for better documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
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
        if (builder.Environment.IsDevelopment())
        {
            // In development allow all origins to simplify local testing
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Production: restrict to known origins (adjust as needed)
            policy.WithOrigins(
                    "https://your-production-blazor-domain",
                    "https://your-production-other-domain"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.UseExceptionHandler();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SSBJr.ndb.Integration API Manager v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

// Map Controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/notifications");

app.MapDefaultEndpoints();

app.Run();