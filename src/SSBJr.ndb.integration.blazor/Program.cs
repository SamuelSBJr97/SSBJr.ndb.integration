using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SSBJr.ndb.integration.Blazor;
using SSBJr.ndb.integration.Blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
// In WebAssembly Blazor, we use the hosting environment's base address
// which should be configured by Aspire to proxy to the correct API service
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register services
builder.Services.AddScoped<IApiInterfaceService, ApiInterfaceService>();

// Add logging
builder.Services.AddLogging();

await builder.Build().RunAsync();
