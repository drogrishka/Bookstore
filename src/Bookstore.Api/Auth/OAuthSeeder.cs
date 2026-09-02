using OpenIddict.Abstractions;

namespace Bookstore.Api.Auth;

using static OpenIddictConstants;

public static class OAuthSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();
        var browserRedirectUris = GetBrowserRedirectUris(configuration, environment).ToArray();

        var machineSecret = configuration["Auth:MachineClientSecret"];
        if (string.IsNullOrWhiteSpace(machineSecret))
        {
            throw new InvalidOperationException("Auth:MachineClientSecret must be configured.");
        }

        if (!environment.IsDevelopment() &&
            string.Equals(machineSecret, "dev-secret-change-me", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The development OAuth client secret cannot be used in Production.");
        }

        if (await manager.FindByClientIdAsync(AuthConstants.MachineClientId, cancellationToken) is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = AuthConstants.MachineClientId,
                ClientSecret = machineSecret,
                ClientType = ClientTypes.Confidential,
                DisplayName = "Bookstore machine client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + AuthConstants.BooksManageScope
                }
            }, cancellationToken);
        }

        if (await manager.FindByClientIdAsync(AuthConstants.BrowserClientId, cancellationToken) is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = AuthConstants.BrowserClientId,
                ClientType = ClientTypes.Public,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = "Bookstore browser client",
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.GrantTypes.Implicit,
                    Permissions.ResponseTypes.Token,
                    Permissions.Prefixes.Scope + AuthConstants.BooksSearchScope
                }
            };

            foreach (var redirectUri in browserRedirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(redirectUri));
            }

            await manager.CreateAsync(descriptor, cancellationToken);
        }
    }

    private static IEnumerable<string> GetBrowserRedirectUris(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var configured = configuration
            .GetSection("Auth:BrowserRedirectUris")
            .Get<string[]>()
            ?.Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (configured is { Length: > 0 })
        {
            return configured;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Auth:BrowserRedirectUris must contain exact redirect URIs in Production.");
        }

        return
        [
            "http://localhost:8080/swagger/oauth2-redirect.html",
            "http://localhost:8080/test-client/callback.html",
            "http://localhost:5044/swagger/oauth2-redirect.html",
            "http://localhost:5044/test-client/callback.html",
            "https://localhost:7044/swagger/oauth2-redirect.html",
            "https://localhost:7044/test-client/callback.html"
        ];
    }
}
