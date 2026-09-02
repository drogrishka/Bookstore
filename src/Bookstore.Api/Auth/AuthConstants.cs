namespace Bookstore.Api.Auth;

public static class AuthConstants
{
    public const string BooksManageScope = "books.manage";
    public const string BooksSearchScope = "books.search";

    public const string BookManagePolicy = "BookManage";
    public const string BookSearchPolicy = "BookSearch";

    public const string GrantClaim = "bookstore_grant";
    public const string ClientCredentialsGrant = "client_credentials";
    public const string ImplicitGrant = "implicit";

    public const string MachineClientId = "bookstore-m2m";
    public const string BrowserClientId = "bookstore-browser";
}
