using Bookstore.Application.Models;

namespace Bookstore.Application.Abstractions;

public interface IBookService
{
    Task<PagedResult<BookDto>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<BookDto?> GetByIdAsync(int bookId, CancellationToken cancellationToken);

    Task<PagedResult<BookDto>> SearchAsync(
        string? title,
        string? author,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BookDto> CreateAsync(BookInput input, CancellationToken cancellationToken);

    Task<BookDto?> UpdateAsync(int bookId, BookInput input, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(int bookId, CancellationToken cancellationToken);
}
