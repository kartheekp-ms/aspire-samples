using System.Reflection;

namespace Sample01_BasicNpgsql.Tests;

public sealed class AppHostSmokeTests
{
    private static string AppHostSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "apphost.cs")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return File.ReadAllText(Path.Combine(dir!.FullName, "apphost.cs"));
    }

    [Fact]
    public void AppHost_declares_postgres_server_and_database()
    {
        var source = AppHostSource();
        Assert.Contains("AddPostgres(\"postgres\")", source);
        Assert.Contains("AddDatabase(\"catalogdb\")", source);
    }

    [Fact]
    public void AppHost_wires_the_consumer_project_to_the_database()
    {
        var source = AppHostSource();
        Assert.Contains("AddProject<Projects.Sample01_BasicNpgsql_Api>", source);
        Assert.Contains("WithReference(catalogDb)", source);
        Assert.Contains("Aspire.Hosting.PostgreSQL", source);
    }
}
