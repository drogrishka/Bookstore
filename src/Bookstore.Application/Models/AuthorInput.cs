using System.ComponentModel.DataAnnotations;

namespace Bookstore.Application.Models;

public sealed class AuthorInput
{
    public required int AuthorId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 3)]
    public required string Name { get; init; }
}
