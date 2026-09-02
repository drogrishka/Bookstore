using System.Security.Claims;
using Bookstore.Api.Auth;
using Bookstore.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Bookstore.Api.Controllers;

using static OpenIddictConstants;

public sealed class AuthorizationController(
    UserManager<ApplicationUser> userManager) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OAuth request cannot be retrieved.");

        var authentication = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            var returnUrl = Request.PathBase + Request.Path + Request.QueryString;
            return Challenge(
                new AuthenticationProperties { RedirectUri = returnUrl },
                IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(authentication.Principal);
        if (user is null || !await userManager.IsEmailConfirmedAsync(user))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity
            .SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(AuthConstants.GrantClaim, AuthConstants.ImplicitGrant);

        identity.SetScopes(
            request.GetScopes()
                .Intersect([AuthConstants.BooksSearchScope], StringComparer.Ordinal));

        identity.SetDestinations(static _ => [Destinations.AccessToken]);

        return SignIn(
            new ClaimsPrincipal(identity),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    public IActionResult Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OAuth request cannot be retrieved.");

        if (!request.IsClientCredentialsGrantType())
        {
            throw new InvalidOperationException("Only client_credentials is supported at the token endpoint.");
        }

        var identity = new ClaimsIdentity(
            authenticationType: TokenValidationParameters.DefaultAuthenticationType,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity
            .SetClaim(Claims.Subject, request.ClientId!)
            .SetClaim(Claims.Name, request.ClientId!)
            .SetClaim(AuthConstants.GrantClaim, AuthConstants.ClientCredentialsGrant);

        identity.SetScopes(request.GetScopes());
        identity.SetDestinations(static _ => [Destinations.AccessToken]);

        return SignIn(
            new ClaimsPrincipal(identity),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
