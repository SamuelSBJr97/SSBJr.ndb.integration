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

// Frontend Blazor Standalone
var blazorApp = builder.AddProject<Projects.SSBJr_ndb_integration_Blazor>("blazorapp")
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
