using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddNpgsqlDataSource("guestbookdb");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
    await using var create = dataSource.CreateCommand(
        """
        CREATE TABLE IF NOT EXISTS guestbook (
            id      SERIAL PRIMARY KEY,
            author  TEXT NOT NULL,
            message TEXT NOT NULL,
            created TIMESTAMPTZ NOT NULL DEFAULT now()
        );
        """);
    await create.ExecuteNonQueryAsync();

    await using var seed = dataSource.CreateCommand(
        """
        INSERT INTO guestbook (author, message)
        SELECT 'Aspire', 'Welcome to the guestbook!'
        WHERE NOT EXISTS (SELECT 1 FROM guestbook);
        """);
    await seed.ExecuteNonQueryAsync();
}

app.MapGet("/", () => "API service is running. Navigate to /entries to see guestbook data.");

app.MapGet("/entries", async (NpgsqlDataSource dataSource) =>
{
    var entries = new List<GuestbookEntry>();
    await using var command = dataSource.CreateCommand(
        "SELECT id, author, message, created FROM guestbook ORDER BY id");
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        entries.Add(new GuestbookEntry(
            reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3)));
    }
    return Results.Ok(entries);
})
.WithName("GetEntries");

app.MapPost("/entries", async (NewGuestbookEntry input, NpgsqlDataSource dataSource) =>
{
    await using var command = dataSource.CreateCommand(
        "INSERT INTO guestbook (author, message) VALUES ($1, $2) RETURNING id, created");
    command.Parameters.AddWithValue(input.Author);
    command.Parameters.AddWithValue(input.Message);
    await using var reader = await command.ExecuteReaderAsync();
    await reader.ReadAsync();
    var entry = new GuestbookEntry(reader.GetInt32(0), input.Author, input.Message, reader.GetDateTime(1));
    return Results.Created($"/entries/{entry.Id}", entry);
})
.WithName("AddEntry");

app.MapDefaultEndpoints();

app.Run();

record GuestbookEntry(int Id, string Author, string Message, DateTime Created);
record NewGuestbookEntry(string Author, string Message);
