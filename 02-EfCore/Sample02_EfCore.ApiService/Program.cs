using Microsoft.EntityFrameworkCore;
using Sample02_EfCore.ApiService;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<TodoDbContext>("todosdb");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!await db.Todos.AnyAsync())
    {
        db.Todos.AddRange(
            new TodoItem { Title = "Learn Aspire", IsComplete = true },
            new TodoItem { Title = "Add PostgreSQL", IsComplete = false });
        await db.SaveChangesAsync();
    }
}

app.MapGet("/", () => "API service is running. Navigate to /todos to see sample data.");

app.MapGet("/todos", async (TodoDbContext db) =>
    await db.Todos.OrderBy(t => t.Id).ToListAsync())
    .WithName("GetTodos");

app.MapPost("/todos", async (TodoItem todo, TodoDbContext db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todos/{todo.Id}", todo);
})
.WithName("AddTodo");

app.MapDefaultEndpoints();

app.Run();
