using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

var baseUrl = args.FirstOrDefault() ?? "http://localhost:8080";
var clientId = Environment.GetEnvironmentVariable("BOOKSTORE_CLIENT_ID") ?? "bookstore-m2m";
var clientSecret = Environment.GetEnvironmentVariable("BOOKSTORE_CLIENT_SECRET") ?? "dev-secret-change-me";

using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

Console.WriteLine($"Bookstore console client -> {baseUrl}");

var tokenResponse = await client.PostAsync(
    "/connect/token",
    new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "client_credentials",
        ["client_id"] = clientId,
        ["client_secret"] = clientSecret,
        ["scope"] = "books.manage"
    }));

var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
if (!tokenResponse.IsSuccessStatusCode)
{
    Console.Error.WriteLine($"Token request failed: {(int)tokenResponse.StatusCode}\n{tokenJson}");
    return 1;
}

using var tokenDocument = JsonDocument.Parse(tokenJson);
var accessToken = tokenDocument.RootElement.GetProperty("access_token").GetString()
    ?? throw new InvalidOperationException("No access_token returned.");

client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", accessToken);

Console.WriteLine("1) Listing books...");
await PrintAsync(await client.GetAsync("/api/books?page=1&pageSize=10"));

var createRequest = new
{
    title = "Patterns of Enterprise Application Architecture",
    subTitle = "Assessment client sample",
    author = new
    {
        authorId = 0,
        name = "Martin Fowler"
    }
};

Console.WriteLine("2) Creating a book...");
var createResponse = await client.PostAsJsonAsync("/api/books", createRequest);
var createBody = await createResponse.Content.ReadAsStringAsync();
Console.WriteLine($"HTTP {(int)createResponse.StatusCode}\n{createBody}");

if (!createResponse.IsSuccessStatusCode)
{
    return 2;
}

using var createdDocument = JsonDocument.Parse(createBody);
var bookId = createdDocument.RootElement.GetProperty("bookId").GetInt32();
var author = createdDocument.RootElement.GetProperty("author");
var authorId = author.GetProperty("authorId").GetInt32();
var authorName = author.GetProperty("name").GetString()!;

Console.WriteLine($"3) Reading book {bookId}...");
await PrintAsync(await client.GetAsync($"/api/books/{bookId}"));

Console.WriteLine($"4) Updating book {bookId}...");
var updateResponse = await client.PutAsJsonAsync(
    $"/api/books/{bookId}",
    new
    {
        title = "Patterns of Enterprise Application Architecture",
        subTitle = "Updated by console client",
        author = new
        {
            authorId,
            name = authorName
        }
    });
await PrintAsync(updateResponse);

Console.WriteLine($"5) Deleting book {bookId}...");
var deleteResponse = await client.DeleteAsync($"/api/books/{bookId}");
Console.WriteLine($"HTTP {(int)deleteResponse.StatusCode}");

Console.WriteLine("Done.");
return 0;

static async Task PrintAsync(HttpResponseMessage response)
{
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine($"HTTP {(int)response.StatusCode}\n{body}\n");
}
