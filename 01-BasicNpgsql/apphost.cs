#:sdk Aspire.AppHost.Sdk@13.4.6
#:package Aspire.Hosting.PostgreSQL@13.4.6
#:project Sample01_BasicNpgsql.Api/Sample01_BasicNpgsql.Api.csproj

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var catalogDb = postgres.AddDatabase("catalogdb");

builder.AddProject<Projects.Sample01_BasicNpgsql_Api>("api")
       .WithReference(catalogDb);

builder.Build().Run();
