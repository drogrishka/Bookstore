using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.Models;

public sealed class BookSearchQuery : PaginationQuery
{
    [StringLength(100)]
    public string? Title { get; init; }

    [StringLength(100)]
    public string? Author { get; init; }
}
