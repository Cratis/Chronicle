// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Provides helper methods for loading TLS certificates.
/// </summary>
public static class CertificateLoader
{
    /// <summary>
    /// Loads the TLS certificate from the top-level TLS configuration.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <returns>The loaded certificate or null if no certificate is available.</returns>
    /// <remarks>
    /// The configured file is read as PKCS#12 whether or not a password is configured, since the TLS listener
    /// needs the private key the PKCS#12 container carries.
    /// </remarks>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when the configured file is not a PKCS#12 file or the configured password is wrong.</exception>
    public static X509Certificate2? LoadCertificate(Configuration.ChronicleOptions options)
    {
        return LoadFromTls(options.Tls);
    }

    static X509Certificate2? LoadFromTls(Configuration.Tls tls)
    {
        if (!tls.Enabled)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(tls.CertificatePath) && File.Exists(tls.CertificatePath))
        {
            // The certificate is always read as PKCS#12, with or without a password. The TLS listener needs the
            // private key, which only PKCS#12 carries, and X509CertificateLoader.LoadCertificateFromFile reads
            // DER/PEM only — handing it a password-less .pfx fails with "ASN1 corrupted data" rather than
            // loading it. LoadPkcs12FromFile takes a nullable password, so it covers both cases.
            return X509CertificateLoader.LoadPkcs12FromFile(tls.CertificatePath, tls.CertificatePassword);
        }

        return null;
    }
}
