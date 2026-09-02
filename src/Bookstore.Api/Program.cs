using Bookstore.Api.Auth;
using Bookstore.Api.Data;
using Bookstore.Api.Infrastructure;
using Bookstore.Infrastructure;
using Bookstore.Infrastructure.Data;
using Bookstore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddEntityFrameworkStores<BookstoreDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.Cookie.Name = builder.Environment.IsDevelopment()
        ? "Bookstore.Identity"
        : "__Host-Bookstore.Identity";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services
    .AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<BookstoreDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token");

        options.AllowClientCredentialsFlow()
            .AllowImplicitFlow();

        options.RegisterScopes(
            AuthConstants.BooksManageScope,
            AuthConstants.BooksSearchScope);

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));

        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            options.AddSigningCertificate(
                CertificateConfiguration.LoadRequiredCertificate(
                    builder.Configuration,
                    "Auth:Certificates:SigningPath",
                    "Auth:Certificates:SigningPassword"));

            options.AddEncryptionCertificate(
                CertificateConfiguration.LoadRequiredCertificate(
                    builder.Configuration,
                    "Auth:Certificates:EncryptionPath",
                    "Auth:Certificates:EncryptionPassword"));
        }

        var aspNetCore = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.BookManagePolicy, policy =>
    {
        policy.AddAuthenticationSchemes(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(
            AuthConstants.GrantClaim,
            AuthConstants.ClientCredentialsGrant);
        policy.RequireAssertion(context =>
            context.User.HasScope(AuthConstants.BooksManageScope));
    });

    options.AddPolicy(AuthConstants.BookSearchPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(
            OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(
            AuthConstants.GrantClaim,
            AuthConstants.ImplicitGrant);
        policy.RequireAssertion(context =>
            context.User.HasScope(AuthConstants.BooksSearchScope));
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BookstoreDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bookstore API",
        Version = "v1",
        Description = "Book CRUD via OAuth client credentials; search via OAuth implicit flow."
    });

    options.AddSecurityDefinition("oauth2-m2m", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Use client_id bookstore-m2m and the configured client secret.",
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri("/connect/token", UriKind.Relative),
                Scopes = new Dictionary<string, string>
                {
                    [AuthConstants.BooksManageScope] = "Create/read/update/delete books"
                }
            }
        }
    });

    options.AddSecurityDefinition("oauth2-implicit", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Legacy implicit flow required by the assignment. Client id: bookstore-browser.",
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("/connect/authorize", UriKind.Relative),
                Scopes = new Dictionary<string, string>
                {
                    [AuthConstants.BooksSearchScope] = "Search books"
                }
            }
        }
    });

    options.OperationFilter<SwaggerSecurityOperationFilter>();
});

var app = builder.Build();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookstore API v1");
        options.OAuthAppName("Bookstore search Swagger UI");
        options.OAuthClientId(AuthConstants.BrowserClientId);
        options.OAuthScopeSeparator(" ");
    });

    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger-m2m";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookstore API v1");
        options.OAuthAppName("Bookstore M2M Swagger UI");
        options.OAuthClientId(AuthConstants.MachineClientId);
        options.OAuthScopeSeparator(" ");
    });
}

app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    await SeedData.InitializeAsync(
        scope.ServiceProvider,
        app.Configuration,
        app.Environment);

    await OAuthSeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration,
        app.Environment);
}

await app.RunAsync();

public partial class Program;
