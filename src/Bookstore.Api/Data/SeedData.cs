using Bookstore.Domain.Entities;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.Api.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var dbContext = services.GetRequiredService<BookstoreDbContext>();

        var initialize = configuration.GetValue(
            "Database:InitializeOnStartup",
            environment.IsDevelopment());

        if (initialize)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "The bookstore database is unavailable. " +
                "Create/apply the schema or enable Database:InitializeOnStartup.");
        }

        var seedEnabled = configuration.GetValue("Seed:Enabled", environment.IsDevelopment());
        if (!seedEnabled)
        {
            return;
        }

        await SeedUserAsync(services, configuration);
        await SeedBooksAsync(dbContext, cancellationToken);
    }

    private static async Task SeedUserAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["Seed:DemoUserEmail"] ?? "demo@bookstore.local";
        var password = configuration["Seed:DemoUserPassword"] ?? "Demo123!";

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Unable to seed demo user: {errors}");
        }
    }

    private static async Task SeedBooksAsync(
        BookstoreDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Books.AnyAsync(cancellationToken))
        {
            return;
        }

        var evans = new Author { Name = "Eric Evans" };
        var martin = new Author { Name = "Robert C. Martin" };
        var fowler = new Author { Name = "Martin Fowler" };

        dbContext.Books.AddRange(
            new Book
            {
                Author = evans,
                Title = "Domain-Driven Design",
                SubTitle = "Tackling Complexity in the Heart of Software"
            },
            new Book
            {
                Author = martin,
                Title = "Clean Code",
                SubTitle = "A Handbook of Agile Software Craftsmanship"
            },
            new Book
            {
                Author = fowler,
                Title = "Refactoring",
                SubTitle = "Improving the Design of Existing Code"
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
