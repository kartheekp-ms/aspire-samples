using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddNpgsqlDataSource("catalogdb");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    await InitializeDatabaseAsync(dataSource);
}

app.MapGet("/catalog", async (NpgsqlDataSource dataSource) =>
{
    var items = new List<CatalogItem>();
    await using var command = dataSource.CreateCommand("SELECT id, name, price FROM catalog ORDER BY id");
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        items.Add(new CatalogItem(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2)));
    }
    return Results.Ok(items);
})
.WithName("GetCatalog");

app.MapPost("/catalog", async (NewCatalogItem input, NpgsqlDataSource dataSource) =>
{
    await using var command = dataSource.CreateCommand(
        "INSERT INTO catalog (name, price) VALUES ($1, $2) RETURNING id");
    command.Parameters.AddWithValue(input.Name);
    command.Parameters.AddWithValue(input.Price);
    var id = (int)(await command.ExecuteScalarAsync())!;
    return Results.Created($"/catalog/{id}", new CatalogItem(id, input.Name, input.Price));
})
.WithName("AddCatalogItem");

app.Run();

static async Task InitializeDatabaseAsync(NpgsqlDataSource dataSource)
{
    await using (var create = dataSource.CreateCommand(
        """
        CREATE TABLE IF NOT EXISTS catalog (
            id    SERIAL PRIMARY KEY,
            name  TEXT NOT NULL,
            price NUMERIC(10, 2) NOT NULL
        );
        """))
    {
        await create.ExecuteNonQueryAsync();
    }

    await using var seed = dataSource.CreateCommand(
        """
        INSERT INTO catalog (name, price)
        SELECT v.name, v.price
        FROM (VALUES ('Widget', 9.99), ('Gadget', 19.99)) AS v(name, price)
        WHERE NOT EXISTS (SELECT 1 FROM catalog);
        """);
    await seed.ExecuteNonQueryAsync();
}

record CatalogItem(int Id, string Name, decimal Price);
record NewCatalogItem(string Name, decimal Price);

public partial class Program;
