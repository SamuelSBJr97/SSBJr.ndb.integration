using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SSBJr.ndb.integration.Blazor;
using SSBJr.ndb.integration.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configurar HttpClient para o API service
// Em Blazor WebAssembly com Aspire, precisamos fazer requisições diretas ao API service
builder.Services.AddScoped(sp => 
{
    var logger = sp.GetRequiredService<ILogger<HttpClient>>();
    
    // Tentar obter URL via configuração do Aspire ou usar fallback
    var apiServiceUrl = GetApiServiceUrl(builder, logger);
    
    var httpClient = new HttpClient { BaseAddress = new Uri(apiServiceUrl) };
    
    // Configurar timeout e headers
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    
    return httpClient;
});

// Register services
builder.Services.AddScoped<IApiInterfaceService, ApiInterfaceService>();

// Add logging
builder.Services.AddLogging();

await builder.Build().RunAsync();

static string GetApiServiceUrl(WebAssemblyHostBuilder builder, ILogger logger)
{
    try
    {
        // 1. Tentar obter via environment variables (configurado pelo Aspire)
        var aspireUrl = Environment.GetEnvironmentVariable("ApiService__BaseUrl");
        if (!string.IsNullOrEmpty(aspireUrl))
        {
            logger.LogInformation("Using Aspire environment variable for API service: {Url}", aspireUrl);
            return aspireUrl;
        }

        // 2. Tentar obter via configuração local
        var configUrl = builder.Configuration["ApiService:BaseUrl"];
        if (!string.IsNullOrEmpty(configUrl))
        {
            logger.LogInformation("Using configuration API service URL: {Url}", configUrl);
            return configUrl;
        }

        // 3. Verificar se estamos em desenvolvimento com Aspire
        var currentUrl = builder.HostEnvironment.BaseAddress;
        var currentUri = new Uri(currentUrl);
        
        // Em Aspire, o API service geralmente roda em porta incrementada
        var apiPort = currentUri.Port == 5000 ? 7080 : currentUri.Port + 1;
        var aspireApiUrl = $"{currentUri.Scheme}://localhost:{apiPort}";
        
        logger.LogInformation("Using calculated Aspire API service URL: {Url}", aspireApiUrl);
        return aspireApiUrl;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error determining API service URL, using fallback");
        return "https://localhost:7080"; // fallback padrão
    }
}
