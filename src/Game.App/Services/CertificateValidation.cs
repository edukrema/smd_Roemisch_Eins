namespace Game.App.Services;

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// The Api uses the ASP.NET Core development certificate, which the Android emulator and the
/// iOS simulator do not trust. Debug builds therefore accept it, but only for localhost.
/// Release builds use the normal certificate validation of the platform.
/// </summary>
public static class CertificateValidation
{
    public static HttpMessageHandler CreateHandler()
    {
        var handler = new HttpClientHandler();
#if DEBUG
        handler.ServerCertificateCustomValidationCallback = (request, certificate, _, errors) =>
            IsValid(request.RequestUri?.Host, certificate, errors);
#endif
        return handler;
    }

    public static bool IsValid(string? host, X509Certificate? certificate, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

#if DEBUG
        // Only the untrusted root is tolerated; a wrong host name is still rejected.
        return host == "localhost"
            && errors == SslPolicyErrors.RemoteCertificateChainErrors
            && certificate?.Subject == "CN=localhost";
#else
        return false;
#endif
    }
}
