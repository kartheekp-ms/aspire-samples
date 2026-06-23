# Aspire.Hosting.PostgreSQL Samples

Three .NET Aspire sample projects, each depending on the
[`Aspire.Hosting.PostgreSQL`](https://www.nuget.org/packages/Aspire.Hosting.PostgreSQL) package
and demonstrating a **distinct** feature of it. Every sample has its own test project.

All samples were scaffolded with the **Aspire CLI** (`aspire new` / `aspire add`) and target a
**C# AppHost**. Two template styles are used for variety: the file-based `aspire-empty` template
and the project-based `aspire-starter` (Blazor) template.

## Solution

`AspireSamples.PostgreSQL.slnx` is the single solution containing every project (the Blazor sample
stacks, the file-based sample's consumer API, and all three test projects). The file-based
`apphost.cs` is included as a solution-folder file item (a file-based AppHost cannot be a `.slnx`
project — see [Testing notes](#testing-notes)).

```bash
dotnet build AspireSamples.PostgreSQL.slnx
dotnet test  AspireSamples.PostgreSQL.slnx   # requires Docker (containers are started)
```

Run an individual sample with the Aspire CLI from its folder, e.g.:

```bash
cd 01-BasicNpgsql && aspire run          # file-based AppHost (apphost.cs)
cd 02-EfCore/Sample02_EfCore.AppHost && aspire run
```

## The samples

| # | Folder | Feature demonstrated | Template | Client integration |
|---|--------|----------------------|----------|--------------------|
| 1 | `01-BasicNpgsql` | `AddPostgres` + `AddDatabase`, consumed with **raw Npgsql** in a minimal API | `aspire-empty` (file-based AppHost) | `Aspire.Npgsql` |
| 2 | `02-EfCore` | Postgres consumed with **EF Core** (`DbContext` + model) from an API + Blazor page | `aspire-starter` (Blazor) | `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` |
| 3 | `03-ImageTag` | **`WithDataVolume()`** persistent PostgreSQL data volume | `aspire-starter` (Blazor) | `Aspire.Npgsql` |

### 1 — `01-BasicNpgsql` (file-based AppHost)
`apphost.cs` declares `AddPostgres("postgres").AddDatabase("catalogdb")` and references the
`Sample01_BasicNpgsql.Api` project via a `#:project` directive + `AddProject<Projects.…>`. The API
uses the `NpgsqlDataSource` registered by `AddNpgsqlDataSource("catalogdb")` to create/seed a
`catalog` table and expose `GET/POST /catalog`.

### 2 — `02-EfCore` (Blazor)
The AppHost adds a `todosdb` database and references the API service. The API registers a
`TodoDbContext` with `AddNpgsqlDbContext<TodoDbContext>("todosdb")`, creates/seeds the schema, and
exposes `GET/POST /todos`. The Blazor `Todos` page renders the data from the API.

### 3 — `03-ImageTag` (Blazor)
The AppHost calls `AddPostgres("postgres").WithDataVolume()` so the Postgres data survives app
restarts. The API stores/reads `guestbook` entries via raw Npgsql; the Blazor `Guestbook` page
renders them.

## Testing notes

- **Blazor samples (2 & 3)** are tested with **`Aspire.Hosting.Testing`**
  (`DistributedApplicationTestingBuilder.CreateAsync<Projects.…_AppHost>()`): app-model tests
  asserting the Postgres and database resources, plus a container-backed end-to-end test that starts
  the app and calls the API (sample 3's end-to-end test also verifies that the persistent data volume
  survives an app restart).
- **File-based sample (1)** cannot use `Aspire.Hosting.Testing` (its `CreateAsync` overloads need an
  AppHost entry-point **Type** from a `.csproj` project reference, which a file-based `apphost.cs`
  does not provide) and cannot be added to the `.slnx` as a project. It is instead covered by a
  container-backed end-to-end test using **`Testcontainers.PostgreSql`** + `WebApplicationFactory`
  against the consumer API, plus source-level smoke assertions on `apphost.cs`.

All end-to-end tests start real PostgreSQL containers, so **Docker must be running**.
