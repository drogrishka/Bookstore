using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Infrastructure.Data;

public sealed class BookstoreDbContext(DbContextOptions<BookstoreDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Author>(entity =>
        {
            entity.HasKey(x => x.AuthorId);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        builder.Entity<Book>(entity =>
        {
            entity.HasKey(x => x.BookId);
            entity.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(x => x.Author)
                .WithMany(x => x.Books)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.Title);
            entity.HasIndex(x => x.AuthorId);
        });
    }
}
