using Testcontainers.PostgreSql;

namespace Sample01_BasicNpgsql.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17.2")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        ConnectionString = _postgres.GetConnectionString();
        Environment.SetEnvironmentVariable("ConnectionStrings__catalogdb", ConnectionString);
    }

    public async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__catalogdb", null);
        await _postgres.DisposeAsync();
    }
}
