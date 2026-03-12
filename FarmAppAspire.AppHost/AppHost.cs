var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var identityDb = builder.AddPostgres("identity-db")
    .WithDataVolume()
    .WithPgAdmin();

var customerDb = builder.AddPostgres("customer-db")
    .WithDataVolume()
    .WithPgAdmin();

var apiService = builder.AddProject<Projects.FarmAppAspire_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

var customerService = builder.AddProject<Projects.FarmAppAspire_CustomerService>("customerservice")
    .WithHttpHealthCheck("/health")
    .WithReference(customerDb)
    .WaitFor(customerDb);

builder.AddProject<Projects.FarmAppAspire_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(customerService)
    .WaitFor(customerService);

builder.Build().Run();
