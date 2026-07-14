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
    /// Creates the server certificate validation callback used for the client's TLS connections.
    /// </summary>
    /// <param name="skipTlsValidation">Whether to skip validation and accept any server certificate.</param>
    /// <param name="pinnedCertificateHash">Optional certificate hash to pin the server certificate to (from a configured client certificate).</param>
    /// <returns>A <see cref="RemoteCertificateValidationCallback"/> that validates the server certificate.</returns>
    /// <remarks>
    /// The default is secure: a certificate that fails validation is rejected. It is accepted only when the
    /// certificate is valid, when <paramref name="skipTlsValidation"/> is set, or when it matches
    /// <paramref name="pinnedCertificateHash"/>. Skipping validation accepts any certificate — including
    /// self-signed ones — so only use it for a trusted server on a trusted network.
    /// </remarks>
    public static RemoteCertificateValidationCallback CreateServerCertificateValidationCallback(bool skipTlsValidation, string? pinnedCertificateHash) =>
        (sender, certificate, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (skipTlsValidation)
            {
                return true;
            }

            if (pinnedCertificateHash is not null && certificate is not null)
            {
                return certificate.GetCertHashString() == pinnedCertificateHash;
            }

            return false;
        };

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
