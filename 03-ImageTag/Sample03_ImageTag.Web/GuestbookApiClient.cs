using System.Net.Http.Json;

namespace Sample03_ImageTag.Web;

public class GuestbookApiClient(HttpClient httpClient)
{
    public async Task<GuestbookEntry[]> GetEntriesAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<GuestbookEntry[]>("/entries", cancellationToken) ?? [];
}

public record GuestbookEntry(int Id, string Author, string Message, DateTime Created);
