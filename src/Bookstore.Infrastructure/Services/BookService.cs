using Bookstore.Application.Abstractions;
using Bookstore.Application.Models;
using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Services;

public sealed class BookService(BookstoreDbContext dbContext) : IBookService
{
    public async Task<PagedResult<BookDto>> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Books
            .AsNoTracking()
            .Include(x => x.Author)
            .OrderBy(x => x.BookId);

        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<BookDto?> GetByIdAsync(int bookId, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .AsNoTracking()
            .Include(x => x.Author)
            .SingleOrDefaultAsync(x => x.BookId == bookId, cancellationToken);

        return book is null ? null : Map(book);
    }

    public async Task<PagedResult<BookDto>> SearchAsync(
        string? title,
        string? author,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Book> query = dbContext.Books
            .AsNoTracking()
            .Include(x => x.Author);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var normalizedTitle = title.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(normalizedTitle));
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            var normalizedAuthor = author.Trim().ToLower();
            query = query.Where(x => x.Author.Name.ToLower().Contains(normalizedAuthor));
        }

        query = query
            .OrderBy(x => x.Title)
            .ThenBy(x => x.BookId);

        return await ToPageAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<BookDto> CreateAsync(BookInput input, CancellationToken cancellationToken)
    {
        var author = await ResolveAuthorAsync(input.Author, cancellationToken);

        var book = new Book
        {
            Author = author,
            AuthorId = author.AuthorId,
            Title = NormalizeRequired(input.Title, "title"),
            SubTitle = NormalizeOptional(input.SubTitle)
        };

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(book);
    }

    public async Task<BookDto?> UpdateAsync(
        int bookId,
        BookInput input,
        CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .Include(x => x.Author)
            .SingleOrDefaultAsync(x => x.BookId == bookId, cancellationToken);

        if (book is null)
        {
            return null;
        }

        var author = await ResolveAuthorAsync(input.Author, cancellationToken);

        book.Author = author;
        book.AuthorId = author.AuthorId;
        book.Title = NormalizeRequired(input.Title, "title");
        book.SubTitle = NormalizeOptional(input.SubTitle);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(book);
    }

    public async Task<bool> DeleteAsync(int bookId, CancellationToken cancellationToken)
    {
        var book = await dbContext.Books
            .SingleOrDefaultAsync(x => x.BookId == bookId, cancellationToken);

        if (book is null)
        {
            return false;
        }

        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<Author> ResolveAuthorAsync(
        AuthorInput input,
        CancellationToken cancellationToken)
    {
        var suppliedName = NormalizeRequired(input.Name, "author.name");

        if (input.AuthorId == 0)
        {
            var normalizedName = suppliedName.ToLower();
            var existingByName = await dbContext.Authors
                .SingleOrDefaultAsync(
                    x => x.Name.ToLower() == normalizedName,
                    cancellationToken);

            if (existingByName is not null)
            {
                return existingByName;
            }

            var author = new Author { Name = suppliedName };
            dbContext.Authors.Add(author);
            return author;
        }

        if (input.AuthorId < 0)
        {
            throw new ArgumentException("author.authorId cannot be negative.");
        }

        var existing = await dbContext.Authors
            .SingleOrDefaultAsync(x => x.AuthorId == input.AuthorId, cancellationToken);

        if (existing is null)
        {
            throw new ArgumentException($"Author {input.AuthorId} does not exist.");
        }

        if (!string.Equals(existing.Name, suppliedName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Author {input.AuthorId} is '{existing.Name}', not '{suppliedName}'.");
        }

        return existing;
    }

    private static async Task<PagedResult<BookDto>> ToPageAsync(
        IQueryable<Book> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BookDto(
                x.BookId,
                new AuthorDto(x.Author.AuthorId, x.Author.Name),
                x.Title,
                x.SubTitle))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResult<BookDto>(items, page, pageSize, totalCount, totalPages);
    }

    private static BookDto Map(Book book) =>
        new(
            book.BookId,
            new AuthorDto(book.Author.AuthorId, book.Author.Name),
            book.Title,
            book.SubTitle);

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrEmpty(normalized) || normalized.Length is < 3 or > 100)
        {
            throw new ArgumentException($"{fieldName} must contain 3 to 100 non-whitespace characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
