// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Provides helper methods for loading TLS certificates for the Chronicle client.
/// </summary>
public static class CertificateLoader
{
    /// <summary>
    /// Loads a certificate based on the priority: ChronicleOptions → Embedded Certificate → Dev Certificate.
    /// </summary>
    /// <param name="certificatePath">The certificate path from options.</param>
    /// <param name="certificatePassword">The certificate password from options.</param>
    /// <returns>The loaded certificate or null if TLS is disabled or no certificate is available.</returns>
    /// <exception cref="CertificateDoesNotExist">Thrown when the specified certificate file does not exist.</exception>
    /// <exception cref="InvalidCertificateOrPassword">Thrown when the specified certificate file is invalid or the password is incorrect.</exception>
    public static X509Certificate2 LoadCertificate(string certificatePath, string certificatePassword)
    {
        if (!File.Exists(certificatePath))
        {
            throw new CertificateDoesNotExist(certificatePath);
        }

        return LoadCertificateFromPath(certificatePath, certificatePassword) ??
            throw new InvalidCertificateOrPassword(certificatePath);
    }

    /// <summary>
    /// Creates a custom certificate validation callback that accepts dev certificates.
    /// </summary>
    /// <param name="clientCertificate">The client certificate if available.</param>
    /// <returns>A validation callback function.</returns>
    public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CreateHttpCertificateValidationCallback(X509Certificate2? clientCertificate)
    {
        return (request, certificate, chain, sslPolicyErrors) =>
        {
            // If there are no errors, the certificate is valid
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            // Accept self-signed certificates (for development)
            if (certificate is not null && clientCertificate is not null)
            {
                return certificate.GetCertHashString() == clientCertificate.GetCertHashString();
            }

            // Accept localhost certificates with name mismatch (for development)
            return sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch;
        };
    }

    static X509Certificate2 LoadCertificateFromPath(string path, string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
#if NET8_0
            return new X509Certificate2(path);
#else
            return X509CertificateLoader.LoadCertificateFromFile(path);
#endif
        }
#if NET8_0
        return new X509Certificate2(path, password);
#else
        return X509CertificateLoader.LoadPkcs12FromFile(path, password);
#endif
    }
}
