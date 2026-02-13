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
    /// Loads a certificate based on the priority: ChronicleOptions → Embedded Certificate → Dev Certificate.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <returns>The loaded certificate or null if no certificate is available.</returns>
    public static X509Certificate2? LoadCertificate(Configuration.ChronicleOptions options)
    {
        if (!string.IsNullOrEmpty(options.Tls.CertificatePath) && File.Exists(options.Tls.CertificatePath))
        {
            return LoadCertificateFromPath(options.Tls.CertificatePath, options.Tls.CertificatePassword);
        }

        return null;
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
