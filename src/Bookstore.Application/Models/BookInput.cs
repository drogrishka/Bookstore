using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.Models;

public sealed class BookInput
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Title { get; init; }

    public string? SubTitle { get; init; }

    [Required]
    public required AuthorInput Author { get; init; }
}
