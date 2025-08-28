var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Configurar o serviço de API com Swagger
var apiService = builder.AddProject<Projects.SSBJr_ndb_integration_Web_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Adicionar React como container Docker usando extensão do Aspire
var reactApp = builder.AddContainer("reactapp", "nginx", "alpine")
    .WithBindMount("../SSBJr.ndb.integration.React/dist", "/usr/share/nginx/html")
    .WithBindMount("../SSBJr.ndb.integration.React/nginx.conf", "/etc/nginx/nginx.conf")
    .WithHttpEndpoint(targetPort: 80, name: "react-http");

// Frontend Blazor
var webFrontend = builder.AddProject<Projects.SSBJr_ndb_integration_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

// Configurar dependências
reactApp.WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
