# Bookstore Backend

ASP.NET Core REST API for managing and searching books.

The project implements the requirements from the bookstore backend assignment, including OAuth2 authentication, pagination, Swagger testing and Docker support.

## Technologies

* .NET 10
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* EF Core InMemory
* OpenIddict
* ASP.NET Core Identity
* Swagger / OpenAPI
* Docker / Docker Compose
* xUnit

## Solution structure

```text
src/
  Bookstore.Api
  Bookstore.Application
  Bookstore.Domain
  Bookstore.Infrastructure
  Bookstore.Client.Console

tests/
  Bookstore.Application.Tests
```

### Bookstore.Domain

Contains the main entities:

* `Book`
* `Author`

### Bookstore.Application

Contains DTOs, request models and the `IBookService` abstraction.

### Bookstore.Infrastructure

Contains Entity Framework Core configuration and the implementation of the book service.

### Bookstore.Api

Contains the REST API, OAuth configuration, authorization policies, Swagger and the browser test client.

### Bookstore.Client.Console

Simple console application for testing the client-credentials flow.

## API

### Book CRUD

CRUD operations are protected with OAuth2 Client Credentials Flow.

```text
GET    /api/books
GET    /api/books/{bookId}
POST   /api/books
PUT    /api/books/{bookId}
DELETE /api/books/{bookId}
```

Required scope:

```text
books.manage
```

### Search

Books can be searched by title and/or author.

```text
GET /api/books/search
```

Example:

```text
GET /api/books/search?title=shadow&author=zafon&page=1&pageSize=20
```

Search supports pagination.

Required scope:

```text
books.search
```

The search endpoint uses the OAuth2 Implicit Flow because it is explicitly required by the assignment.

For a new browser application I would normally use Authorization Code Flow with PKCE instead.

## Object model

Example book response:

```json
{
  "bookId": 1,
  "author": {
    "authorId": 1,
    "name": "Carlos Ruiz Zafon"
  },
  "title": "The Shadow of the Wind",
  "subTitle": null
}
```

Validation rules:

```text
Author.name    required, 3-100 characters
Book.title     required, 3-100 characters
Book.author    required
Book.subTitle  optional
```

## Running with Docker

Docker is the easiest way to start the complete application.

Copy the environment template:

```powershell
Copy-Item .env.example .env
```

On Linux/macOS:

```bash
cp .env.example .env
```

Set local passwords in `.env` and then run:

```bash
docker compose up --build
```

The application will be available at:

```text
API:            http://localhost:8080
Swagger Search: http://localhost:8080/swagger
Swagger CRUD:   http://localhost:8080/swagger-m2m
Test Client:    http://localhost:8080/test-client/
Health:         http://localhost:8080/health
```

Docker Compose also starts SQL Server.

To remove the containers and database volume:

```bash
docker compose down -v
```

## Running from Visual Studio / VS Code

The Development configuration uses EF Core InMemory, so SQL Server is not required.

Configure the local secrets:

```bash
dotnet user-secrets set "Auth:MachineClientSecret" "YOUR_CLIENT_SECRET" --project src/Bookstore.Api

dotnet user-secrets set "Seed:DemoUserPassword" "YOUR_DEMO_PASSWORD" --project src/Bookstore.Api
```

Then run:

```bash
dotnet restore
dotnet run --project src/Bookstore.Api
```

Development URLs:

```text
https://localhost:7044
http://localhost:5044
```

## OAuth clients

Two OAuth clients are configured.

### Machine client

```text
Client ID: bookstore-m2m
Flow:      Client Credentials
Scope:     books.manage
```

This client is used for Book CRUD operations.

The client secret is configured locally and is not stored in the repository.

### Browser client

```text
Client ID: bookstore-browser
Flow:      Implicit
Scope:     books.search
```

This client is used by Swagger and the browser test client for authenticated book searches.

## Getting a CRUD token

Example:

```bash
curl -X POST http://localhost:8080/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=bookstore-m2m&client_secret=YOUR_SECRET&scope=books.manage"
```

Use the returned access token:

```bash
curl http://localhost:8080/api/books \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

## Swagger

There are two Swagger UI routes because the API demonstrates two different OAuth flows.

For CRUD:

```text
http://localhost:8080/swagger-m2m
```

Use:

```text
client_id: bookstore-m2m
scope:     books.manage
```

For search:

```text
http://localhost:8080/swagger
```

Use:

```text
client_id: bookstore-browser
scope:     books.search
```

## Database

Two EF Core providers are supported.

`InMemory` is used for simple local development.

`SqlServer` is used by Docker Compose.

The database provider can be selected through configuration:

```text
Database:Provider
```

Demo data is only inserted when:

```text
Seed:Enabled=true
```

## Tests

Run the tests with:

```bash
dotnet test
```

The current unit tests cover book retrieval, search filters, pagination and validation-related service behavior.

## Design decisions

### Separate application layers

The solution separates API, application logic, domain objects and persistence so that HTTP and database concerns do not need to be mixed with the core models.

### Two OAuth flows

The assignment requires Client Credentials for CRUD and Implicit Flow for search, so separate clients and authorization policies are used for the two cases.

### Scope and grant validation

The API checks both the required OAuth scope and the flow that created the token. This prevents a search token from being used for CRUD operations, or a machine token from being used for interactive search.

### InMemory and SQL Server

InMemory makes the project easy to start directly from Visual Studio, while SQL Server in Docker provides a persistent relational database without requiring a local SQL Server installation.

## Notes

The OAuth2 Implicit Flow is included to match the assignment requirements. For a new frontend application, Authorization Code Flow with PKCE would be the preferred approach.

Secrets, local environment files, build output and development certificates are excluded from source control.
