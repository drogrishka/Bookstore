using Bookstore.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bookstore.Api.Infrastructure;

public sealed class SwaggerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorizeAttributes = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(inherit: true)
            .Union(context.MethodInfo.GetCustomAttributes(inherit: true))
            .OfType<AuthorizeAttribute>()
            .ToArray() ?? [];

        var policy = authorizeAttributes
            .Select(attribute => attribute.Policy)
            .FirstOrDefault(value => value is AuthConstants.BookManagePolicy or AuthConstants.BookSearchPolicy);

        if (policy is null)
        {
            return;
        }

        operation.Responses ??= [];
        operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });

        var (schemeName, scope) = policy == AuthConstants.BookManagePolicy
            ? ("oauth2-m2m", AuthConstants.BooksManageScope)
            : ("oauth2-implicit", AuthConstants.BooksSearchScope);

        var scheme = new OpenApiSecuritySchemeReference(schemeName, context.Document);
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [scheme] = [scope]
            }
        ];
    }
}
