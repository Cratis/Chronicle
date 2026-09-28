// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Chooses the certificate, if any, used by the Chronicle and dedicated health listeners at startup.
/// </summary>
internal static class KernelCertificateSelection
{
    /// <summary>
    /// Loads the configured certificate or, in a development build with TLS on the main port,
    /// generates a self-signed certificate. The caller keeps it alive for the listener lifetime.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="logger">The kernel logger.</param>
    /// <param name="developmentBuild">Whether the process was compiled with the DEVELOPMENT symbol.</param>
    /// <returns>The selected certificate, or null when no listener certificate is available.</returns>
    internal static X509Certificate2? Select(ChronicleOptions options, ILogger<Kernel> logger, bool developmentBuild)
    {
        // The dedicated health listener can still use the top-level TLS certificate even when
        // the main listener has explicitly opted into cleartext h2c.
        var certificate = CertificateLoader.LoadCertificate(options);
        if (!options.Tls.Enabled && options.DedicatedHealthPort is not null && options.Health.Tls)
        {
            certificate = CertificateLoader.LoadHealthCertificate(options);
        }

        if (certificate is not null && options.Tls.Enabled)
        {
            logger.TlsCertificateLoaded();
        }
        else if (certificate is null && options.Tls.Enabled)
        {
            if (developmentBuild)
            {
                certificate = DevelopmentCertificate.Create();
                logger.DevelopmentCertificateGenerated();
            }
            else
            {
                logger.TlsCertificateMissingProduction();
            }
        }

        return certificate;
    }
}
