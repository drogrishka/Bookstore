# Bookstore backend (.NET 10)

Production-oriented reference implementation of the requested bookstore back-end.

## Requirements covered

- .NET 10 LTS / ASP.NET Core.
- Layered solution: `Domain`, `Application`, `Infrastructure`, `Api`.
- Requested `Author` and `Book` object model and validation.
- `Book` CRUD REST API.
- Search by title and/or author with pagination.
- OAuth 2.0 authorization server implemented with OpenIddict.
- CRUD accepts only **client credentials** tokens with `books.manage`.
- Search accepts only **implicit-flow** tokens with `books.search`.
- An additional `bookstore_grant` token claim prevents a token from the wrong flow being reused even if a scope is accidentally over-granted.
- ASP.NET Core Identity login for the interactive implicit flow.
- Swagger/OpenAPI configured with operation-specific OAuth schemes.
- Browser test client for implicit search.
- Console test client for client-credentials CRUD.
- EF Core **SQL Server** provider for Docker/production-style execution.
- EF Core **InMemory** provider for zero-setup direct development/testing.
- Docker/Docker Compose, including SQL Server 2025 Developer container.
- Health check, Problem Details exception handling and unit tests.

> The OAuth 2.0 implicit flow is a legacy flow and is not recommended for new browser applications. It is implemented
> because it is explicitly required by the assignment. For a new system, prefer Authorization Code + PKCE.

## Fastest start: Docker Desktop

From the repository root:

```bash
docker compose up --build
```

Docker Compose starts:

- the API at `http://localhost:8080`;
- SQL Server 2025 Developer on host port `14333`;
- a persistent SQL Server data volume.

Open:

- Search Swagger (implicit client preconfigured): `http://localhost:8080/swagger`
- CRUD Swagger (M2M client preconfigured): `http://localhost:8080/swagger-m2m`
- Browser implicit-flow client: `http://localhost:8080/test-client/`
- Health: `http://localhost:8080/health`

No local SQL Server installation is needed.

To reset all Docker data:

```bash
docker compose down -v
```

## Visual Studio / VS Code: zero database setup

Requirements:

- .NET 10 SDK
- Visual Studio 2026+ with the ASP.NET workload, or VS Code + C# Dev Kit

Development configuration uses EF Core InMemory, so direct execution requires no database service:

```bash
dotnet restore
dotnet run --project src/Bookstore.Api
```

Launch profiles expose:

- `https://localhost:7044`
- `http://localhost:5044`

The repository also includes `Bookstore.slnLaunch`, a shared Visual Studio solution launch profile that starts `Bookstore.Api`. The Visual Studio HTTPS project profile opens Swagger automatically.

## Development credentials

The following values are intentionally development-only.

### Machine-to-machine CRUD client

- client id: `bookstore-m2m`
- client secret: `dev-secret-change-me`
- scope: `books.manage`
- token endpoint: `/connect/token`

Request a token:

```bash
curl -X POST http://localhost:8080/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=bookstore-m2m&client_secret=dev-secret-change-me&scope=books.manage"
```

Use the returned token:

```bash
curl http://localhost:8080/api/books/1 \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

Create a book:

```bash
curl -X POST http://localhost:8080/api/books \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Domain-Driven Design",
    "subTitle": "Tackling Complexity in the Heart of Software",
    "author": {
      "authorId": 1,
      "name": "Eric Evans"
    }
  }'
```

### Interactive search client

- client id: `bookstore-browser`
- scope: `books.search`
- authorization endpoint: `/connect/authorize`

Seeded development user:

- email: `demo@bookstore.local`
- password: `Demo123!`

Open `http://localhost:8080/test-client/`, select **Sign in / get search token**, sign in, and run a search.

Example endpoint:

```text
GET /api/books/search?title=clean&author=martin&page=1&pageSize=20
```

`title` and `author` are optional and can be combined. `pageSize` is limited to 1..100.

## Swagger OAuth testing

Swagger contains two OAuth definitions and assigns them only to the operations that need them. Because Swagger UI has one default OAuth client id per UI instance, two UI routes are provided over the same OpenAPI document.

For CRUD, open `http://localhost:8080/swagger-m2m` and authorize `oauth2-m2m` with:

- client id `bookstore-m2m`
- client secret `dev-secret-change-me`
- scope `books.manage`

For search, open `http://localhost:8080/swagger` and authorize `oauth2-implicit` with:

- client id `bookstore-browser`
- scope `books.search`
- development login shown above

Server-side policies independently enforce the flow marker and scope, so Swagger metadata is not relied upon for security.

## Console test client

With the API running on Docker:

```bash
dotnet run --project src/Bookstore.Client.Console -- http://localhost:8080
```

The client obtains a client-credentials token, then exercises list/create/read/update/delete.

Optional environment-variable overrides:

- `BOOKSTORE_CLIENT_ID`
- `BOOKSTORE_CLIENT_SECRET`

For a direct Visual Studio HTTP launch, pass `http://localhost:5044` instead.

## REST API

### CRUD — client credentials only

- `GET /api/books?page=1&pageSize=20`
- `GET /api/books/{bookId}`
- `POST /api/books`
- `PUT /api/books/{bookId}`
- `DELETE /api/books/{bookId}`

Required: scope `books.manage` and grant marker `client_credentials`.

### Search — implicit flow only

- `GET /api/books/search?title={title}&author={author}&page=1&pageSize=20`

Required: scope `books.search` and grant marker `implicit`.

## Object model

Responses follow the requested shape:

```json
{
  "bookId": 1,
  "author": {
    "authorId": 1,
    "name": "Eric Evans"
  },
  "title": "Domain-Driven Design",
  "subTitle": "Tackling Complexity in the Heart of Software"
}
```

Validation:

- `Author.authorId`: `int32`, required in the response model
- `Author.name`: required, 3..100 characters
- `Book.bookId`: `int32`, required in the response model
- `Book.author`: required
- `Book.title`: required, 3..100 characters
- `Book.subTitle`: optional string

For create/update, a positive existing `authorId` must match the supplied author name. `authorId = 0` resolves an
existing author with the same name or creates a new author.

## Database modes

`Database:Provider` supports exactly two values:

- `InMemory` — default in `appsettings.Development.json`; ideal for opening the solution and pressing Run.
- `SqlServer` — used by Docker Compose and intended for persistent/production-style execution.

Docker Compose uses SQL Server 2025 Developer and persists `/var/opt/mssql` in the `bookstore-sql-data` volume.

For local SQL Server outside Docker, set for example:

```text
Database__Provider=SqlServer
ConnectionStrings__Bookstore=Server=localhost,14333;Database=Bookstore;User Id=sa;Password=...;Encrypt=True;TrustServerCertificate=True
```

## Database lifecycle and migrations

For zero-preparation Development/Docker evaluation, `Database:InitializeOnStartup=true` uses `EnsureCreated()`. Demo
books/user data are seeded only when `Seed:Enabled=true`; OAuth client bootstrap is handled separately.

For a real production rollout:

1. set `Database:InitializeOnStartup=false`;
2. use `Database:Provider=SqlServer`;
3. create/review/apply EF Core migrations as part of deployment.

Example commands:

```bash
dotnet ef migrations add InitialCreate \
  --project src/Bookstore.Infrastructure \
  --startup-project src/Bookstore.Api

dotnet ef database update \
  --project src/Bookstore.Infrastructure \
  --startup-project src/Bookstore.Api
```

The infrastructure project includes `Microsoft.EntityFrameworkCore.Design` and a design-time DbContext factory.

## Run tests

```bash
dotnet test Bookstore.sln
```

The unit tests use an isolated EF Core InMemory database and cover search filters, pagination and missing-book behavior.

## Production configuration

When `ASPNETCORE_ENVIRONMENT=Production`:

- configure `Database:Provider=SqlServer` and a production connection string;
- the known development machine-client secret is rejected;
- OpenIddict development certificates are not used;
- provide signing and encryption PFX certificates;
- OpenIddict keeps the HTTPS requirement enabled;
- the embedded Swagger/test-client UI is not served;
- use only exact production redirect URIs;
- keep `Database:InitializeOnStartup=false` and deploy reviewed migrations.

Relevant environment-variable names:

```text
Database__Provider
ConnectionStrings__Bookstore
Database__InitializeOnStartup
Seed__Enabled
Auth__MachineClientSecret
Auth__BrowserRedirectUris__0
Auth__Certificates__SigningPath
Auth__Certificates__SigningPassword
Auth__Certificates__EncryptionPath
Auth__Certificates__EncryptionPassword
```

When deployed behind a reverse proxy, also configure forwarded headers and trusted proxies for that environment instead of
blindly trusting all forwarded headers.

## Solution structure

```text
Bookstore.sln
src/
  Bookstore.Domain/          entities
  Bookstore.Application/     DTOs, request models, service contracts
  Bookstore.Infrastructure/  EF Core, Identity persistence, book service
  Bookstore.Api/             REST API, OAuth/OIDC server, Swagger, web test client
  Bookstore.Client.Console/  client-credentials test client
tests/
  Bookstore.Application.Tests/
```

## Design choices

- **OpenIddict instead of Duende IdentityServer:** fulfills both required OAuth flows without introducing a commercial
  production-license dependency for this solution.
- **Authorization server and resource API in one ASP.NET Core host:** minimizes startup steps while preserving OAuth token
  issuance and validation boundaries; the application layers are still separate.
- **SQL Server + InMemory:** SQL Server covers persistent/production-style use and the assignment requirement; InMemory
  gives true one-click local development.
- **DTOs instead of EF entities:** avoids persistence coupling and over-posting.
- **Flow + scope policies:** an implicit token cannot call CRUD and a machine token cannot call user search.
- **No secret/certificate fallback in Production:** deployment fails fast when required security material is missing.
