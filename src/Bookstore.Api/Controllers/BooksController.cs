using Bookstore.Api.Auth;
using Bookstore.Application.Abstractions;
using Bookstore.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookstore.Api.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(IBookService bookService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthConstants.BookManagePolicy)]
    [ProducesResponseType<PagedResult<BookDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<BookDto>> GetPage(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken) =>
        bookService.GetPageAsync(query.Page, query.PageSize, cancellationToken);

    [HttpGet("{bookId:int}")]
    [Authorize(Policy = AuthConstants.BookManagePolicy)]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> GetById(
        int bookId,
        CancellationToken cancellationToken)
    {
        var book = await bookService.GetByIdAsync(bookId, cancellationToken);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [Authorize(Policy = AuthConstants.BookManagePolicy)]
    [ProducesResponseType<BookDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> Create(
        [FromBody] BookInput input,
        CancellationToken cancellationToken)
    {
        var created = await bookService.CreateAsync(input, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { bookId = created.BookId },
            created);
    }

    [HttpPut("{bookId:int}")]
    [Authorize(Policy = AuthConstants.BookManagePolicy)]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDto>> Update(
        int bookId,
        [FromBody] BookInput input,
        CancellationToken cancellationToken)
    {
        var updated = await bookService.UpdateAsync(bookId, input, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{bookId:int}")]
    [Authorize(Policy = AuthConstants.BookManagePolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int bookId,
        CancellationToken cancellationToken)
    {
        var deleted = await bookService.DeleteAsync(bookId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthConstants.BookSearchPolicy)]
    [ProducesResponseType<PagedResult<BookDto>>(StatusCodes.Status200OK)]
    public Task<PagedResult<BookDto>> Search(
        [FromQuery] BookSearchQuery query,
        CancellationToken cancellationToken) =>
        bookService.SearchAsync(
            query.Title,
            query.Author,
            query.Page,
            query.PageSize,
            cancellationToken);
}
