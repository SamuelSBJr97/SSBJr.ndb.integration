var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.SSBJr_ndb_integration_Web_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.SSBJr_ndb_integration_Web_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
