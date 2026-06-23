var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var guestbookDb = postgres.AddDatabase("guestbookdb");

var apiService = builder.AddProject<Projects.Sample03_ImageTag_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(guestbookDb)
    .WaitFor(guestbookDb);

builder.AddProject<Projects.Sample03_ImageTag_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
