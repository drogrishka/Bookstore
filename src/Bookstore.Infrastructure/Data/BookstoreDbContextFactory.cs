using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookstore.Infrastructure.Data;

public sealed class BookstoreDbContextFactory : IDesignTimeDbContextFactory<BookstoreDbContext>
{
    public BookstoreDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookstoreDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=Bookstore;Trusted_Connection=True;TrustServerCertificate=True")
            .UseOpenIddict()
            .Options;

        return new BookstoreDbContext(options);
    }
}
