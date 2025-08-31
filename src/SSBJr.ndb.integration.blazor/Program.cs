using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SSBJr.ndb.integration.Blazor;
using SSBJr.ndb.integration.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add configuration
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ApiSettings:BaseUrl"] = "https://localhost:7080"
});

// Register HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register additional HttpClient for API calls
builder.Services.AddScoped<HttpClient>(sp =>
{
    var client = new HttpClient();
    client.BaseAddress = new Uri("https://localhost:7080");
    return client;
});

// Register API Interface Service
builder.Services.AddScoped<IApiInterfaceService, ApiInterfaceService>();

// Add logging
builder.Services.AddLogging();

await builder.Build().RunAsync();
