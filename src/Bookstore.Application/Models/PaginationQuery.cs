using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.Models;

public class PaginationQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
