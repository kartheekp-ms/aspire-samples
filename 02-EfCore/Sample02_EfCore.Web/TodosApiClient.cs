using System.Net.Http.Json;

namespace Sample02_EfCore.Web;

public class TodosApiClient(HttpClient httpClient)
{
    public async Task<TodoItem[]> GetTodosAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<TodoItem[]>("/todos", cancellationToken) ?? [];
}

public record TodoItem(int Id, string Title, bool IsComplete);
