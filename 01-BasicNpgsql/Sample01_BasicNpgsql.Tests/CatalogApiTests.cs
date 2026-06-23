using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Sample01_BasicNpgsql.Tests;

public sealed class CatalogApiTests : IClassFixture<PostgresFixture>
{
    public CatalogApiTests(PostgresFixture fixture) => _ = fixture;

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b.UseEnvironment("Development"));

    [Fact]
    public async Task Get_catalog_returns_seeded_items()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var items = await client.GetFromJsonAsync<List<CatalogItemDto>>("/catalog");

        Assert.NotNull(items);
        Assert.True(items!.Count >= 2);
        Assert.Contains(items, i => i.Name == "Widget");
        Assert.Contains(items, i => i.Name == "Gadget");
    }

    [Fact]
    public async Task Post_catalog_persists_new_item()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/catalog", new { Name = "Sprocket", Price = 4.50m });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var items = await client.GetFromJsonAsync<List<CatalogItemDto>>("/catalog");
        Assert.NotNull(items);
        Assert.Contains(items!, i => i.Name == "Sprocket" && i.Price == 4.50m);
    }

    private sealed record CatalogItemDto(int Id, string Name, decimal Price);
}
