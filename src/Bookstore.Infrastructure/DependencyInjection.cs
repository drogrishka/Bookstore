using Bookstore.Application.Abstractions;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookstore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";

        services.AddDbContext<BookstoreDbContext>(options =>
        {
            if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("Bookstore");
            }
            else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("Bookstore");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "ConnectionStrings:Bookstore is required when Database:Provider=SqlServer.");
                }

                options.UseSqlServer(
                    connectionString,
                    sqlServer => sqlServer.EnableRetryOnFailure());
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported database provider '{provider}'. Use 'InMemory' or 'SqlServer'.");
            }

            options.UseOpenIddict();
        });

        services.AddScoped<IBookService, BookService>();

        return services;
    }
}
