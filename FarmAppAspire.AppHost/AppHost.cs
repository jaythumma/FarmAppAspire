var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var identityDb = builder.AddPostgres("identity-db");

var apiService = builder.AddProject<Projects.FarmAppAspire_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.FarmAppAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(identityDb)
    .WaitFor(identityDb);

builder.Build().Run();
