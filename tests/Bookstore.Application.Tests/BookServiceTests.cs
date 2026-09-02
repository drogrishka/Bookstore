using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookstore.Application.Tests;

public sealed class BookServiceTests
{
    [Fact]
    public async Task Search_filters_by_title_and_author_and_paginates()
    {
        var options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseOpenIddict()
            .Options;

        await using var db = new BookstoreDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var martin = new Author { Name = "Robert C. Martin" };
        var evans = new Author { Name = "Eric Evans" };

        db.Books.AddRange(
            new Book { Author = martin, Title = "Clean Code" },
            new Book { Author = martin, Title = "Clean Architecture" },
            new Book { Author = evans, Title = "Domain-Driven Design" });

        await db.SaveChangesAsync();

        var service = new BookService(db);

        var result = await service.SearchAsync(
            title: "clean",
            author: "martin",
            page: 1,
            pageSize: 1,
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Contains("Clean", result.Items[0].Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Martin", result.Items[0].Author.Name, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task Create_rejects_title_that_is_too_short_after_trimming()
    {
        var options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseOpenIddict()
            .Options;

        await using var db = new BookstoreDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = new BookService(db);
        var input = new Bookstore.Application.Models.BookInput
        {
            Title = "  A  ",
            Author = new Bookstore.Application.Models.AuthorInput
            {
                AuthorId = 0,
                Name = "Valid Author"
            }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(input, CancellationToken.None));

        Assert.Contains("title", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_book()
    {
        var options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseOpenIddict()
            .Options;

        await using var db = new BookstoreDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var service = new BookService(db);

        var result = await service.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
    }
}
