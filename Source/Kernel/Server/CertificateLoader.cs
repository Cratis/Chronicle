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
    /// Loads the gRPC TLS certificate from the top-level TLS configuration.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <returns>The loaded certificate or null if no certificate is available.</returns>
    public static X509Certificate2? LoadCertificate(Configuration.ChronicleOptions options)
    {
        return LoadFromTls(options.Tls);
    }

    /// <summary>
    /// Loads the Workbench TLS certificate using the Workbench TLS fallback chain.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <returns>The loaded certificate or null if no certificate is available.</returns>
    public static X509Certificate2? LoadWorkbenchCertificate(Configuration.ChronicleOptions options)
    {
        var workbenchTls = options.WorkbenchTls;
        return LoadFromTls(workbenchTls);
    }

    static X509Certificate2? LoadFromTls(Configuration.Tls tls)
    {
        if (!tls.Enabled)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(tls.CertificatePath) && File.Exists(tls.CertificatePath))
        {
            return LoadCertificateFromPath(tls.CertificatePath, tls.CertificatePassword);
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
