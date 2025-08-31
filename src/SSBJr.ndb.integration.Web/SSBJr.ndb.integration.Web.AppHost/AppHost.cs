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

// NOTA: React App desabilitado temporariamente para evitar problemas de build
// Para executar o React manualmente:
// 1. cd SSBJr.ndb.integration.React
// 2. npm install
// 3. npm run dev
Console.WriteLine("React App está desabilitado no AppHost. Execute manualmente com: cd SSBJr.ndb.integration.React && npm run dev");

builder.Build().Run();
