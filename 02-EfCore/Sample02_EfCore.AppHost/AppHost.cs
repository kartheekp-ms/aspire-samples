var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var todosDb = postgres.AddDatabase("todosdb");

var apiService = builder.AddProject<Projects.Sample02_EfCore_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(todosDb)
    .WaitFor(todosDb);

builder.AddProject<Projects.Sample02_EfCore_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
