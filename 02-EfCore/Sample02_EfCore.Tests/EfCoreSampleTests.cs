using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Sample02_EfCore.Tests;

public class EfCoreSampleTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    [Fact]
    public async Task AppModel_declares_postgres_server_and_todos_database()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sample02_EfCore_AppHost>();

        var resources = appHost.Resources;

        var postgres = Assert.Single(resources.OfType<PostgresServerResource>());
        Assert.Equal("postgres", postgres.Name);

        var database = Assert.Single(resources.OfType<PostgresDatabaseResource>());
        Assert.Equal("todosdb", database.Name);
    }

    [Fact]
    public async Task ApiService_returns_seeded_todos_from_postgres()
    {
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sample02_EfCore_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var todos = await httpClient.GetFromJsonAsync<List<TodoDto>>("/todos", cancellationToken);

        Assert.NotNull(todos);
        Assert.Contains(todos!, t => t.Title == "Learn Aspire");
        Assert.Contains(todos!, t => t.Title == "Add PostgreSQL");
    }

    private sealed record TodoDto(int Id, string Title, bool IsComplete);
}
