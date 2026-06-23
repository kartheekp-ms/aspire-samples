using System.Diagnostics;
using System.Net.Http.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;

namespace Sample03_ImageTag.Tests;

public class ImageTagSampleTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task AppModel_declares_postgres_server_and_database()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sample03_ImageTag_AppHost>();

        var resources = appHost.Resources;

        var postgres = Assert.Single(resources.OfType<PostgresServerResource>());
        Assert.Equal("postgres", postgres.Name);

        Assert.Single(resources.OfType<PostgresDatabaseResource>(), db => db.Name == "guestbookdb");
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task DataVolume_persists_guestbook_entries_across_app_restarts()
    {
        var cancellationToken = CancellationToken.None;
        var marker = $"persist-{Guid.NewGuid():N}";
        string? volumeName = null;

        try
        {
            {
                var appHost = await DistributedApplicationTestingBuilder
                    .CreateAsync<Projects.Sample03_ImageTag_AppHost>(cancellationToken);

                var postgres = appHost.Resources.OfType<PostgresServerResource>().Single();
                volumeName = postgres.Annotations.OfType<ContainerMountAnnotation>()
                    .Single(mount => mount.Type == ContainerMountType.Volume).Source;

                TryRemoveDockerVolume(volumeName!);

                await using var app = await appHost.BuildAsync(cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);
                await app.StartAsync(cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);

                using var httpClient = app.CreateHttpClient("apiservice");
                await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);

                var response = await httpClient.PostAsJsonAsync(
                    "/entries", new { author = "PersistTest", message = marker }, cancellationToken);
                response.EnsureSuccessStatusCode();
            } // app disposed here -> container removed, volume released

            {
                var appHost = await DistributedApplicationTestingBuilder
                    .CreateAsync<Projects.Sample03_ImageTag_AppHost>(cancellationToken);

                await using var app = await appHost.BuildAsync(cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);
                await app.StartAsync(cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);

                using var httpClient = app.CreateHttpClient("apiservice");
                await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
                    .WaitAsync(StartupTimeout, cancellationToken);

                var entries = await httpClient.GetFromJsonAsync<List<GuestbookEntryDto>>("/entries", cancellationToken);

                Assert.NotNull(entries);
                Assert.Contains(entries!, e => e.Message == marker);
            }
        }
        finally
        {
            if (volumeName is not null)
            {
                TryRemoveDockerVolume(volumeName);
            }
        }
    }

    private static void TryRemoveDockerVolume(string volumeName)
    {
        try
        {
            var startInfo = new ProcessStartInfo("docker", $"volume rm -f {volumeName}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit(milliseconds: 30_000);
        }
        catch
        {
        }
    }

    [Fact]
    [Trait("Category", "RequiresDocker")]
    public async Task ApiService_returns_seeded_guestbook_entries_from_postgres()
    {
        var cancellationToken = CancellationToken.None;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Sample03_ImageTag_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        using var httpClient = app.CreateHttpClient("apiservice");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);

        var entries = await httpClient.GetFromJsonAsync<List<GuestbookEntryDto>>("/entries", cancellationToken);

        Assert.NotNull(entries);
        Assert.Contains(entries!, e => e.Author == "Aspire");
    }

    private sealed record GuestbookEntryDto(int Id, string Author, string Message, DateTime Created);
}
