namespace Bookstore.Application.Models;

public sealed record BookDto(
    int BookId,
    AuthorDto Author,
    string Title,
    string? SubTitle);
