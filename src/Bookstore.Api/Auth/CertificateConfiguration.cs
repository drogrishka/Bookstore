using System.Security.Cryptography.X509Certificates;

namespace Bookstore.Api.Auth;

internal static class CertificateConfiguration
{
    public static X509Certificate2 LoadRequiredCertificate(
        IConfiguration configuration,
        string pathKey,
        string passwordKey)
    {
        var path = configuration[pathKey];
        var password = configuration[passwordKey];

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"Required certificate configuration '{pathKey}' is missing.");
        }

        return X509CertificateLoader.LoadPkcs12FromFile(
            path,
            password,
            X509KeyStorageFlags.EphemeralKeySet);
    }
}
