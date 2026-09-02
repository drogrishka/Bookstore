using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models.Account;

public sealed class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}
