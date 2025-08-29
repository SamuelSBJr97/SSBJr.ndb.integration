var builder = DistributedApplication.CreateBuilder(args);

// Adicionar Redis para cache
var cache = builder.AddRedis("cache");

// Adicionar PostgreSQL para persistência
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("apimanager");

// Configurar o serviço de API
var apiService = builder.AddProject<Projects.SSBJr_ndb_integration_Web_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(cache)
    .WithReference(postgres)
    .WaitFor(cache)
    .WaitFor(postgres);

// Frontend Blazor Principal
var webFrontend = builder.AddProject<Projects.SSBJr_ndb_integration_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WithReference(postgres)
    .WithReference(apiService)
    .WaitFor(cache)
    .WaitFor(postgres)
    .WaitFor(apiService);

// Frontend Blazor Standalone
var blazorApp = builder.AddProject<Projects.SSBJr_ndb_integration_Blazor>("blazorapp")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

// React App usando Dockerfile (mais estável)
try 
{
    var reactApp = builder.AddDockerfile("reactapp", "../SSBJr.ndb.integration.React")
        .WithHttpEndpoint(targetPort: 80, name: "react-http")
        .WithReference(apiService)
        .WaitFor(apiService);
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not add React app: {ex.Message}");
    Console.WriteLine("React app will be skipped. You can run it manually with: npm run dev");
}

builder.Build().Run();
