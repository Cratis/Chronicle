// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Chooses and validates the transport for the Chronicle and dedicated health listeners.
/// </summary>
internal static class KernelListeners
{
    /// <summary>
    /// Configures listeners after checking the certificate and authentication requirements.
    /// Passing a certificate to <paramref name="listen"/> selects HTTPS; passing null selects cleartext.
    /// </summary>
    /// <param name="options">The Chronicle options.</param>
    /// <param name="certificate">The TLS certificate, if available.</param>
    /// <param name="logger">The kernel logger.</param>
    /// <param name="listen">Registers one listener with the selected protocols and certificate.</param>
    /// <exception cref="InvalidOperationException">Thrown when a required certificate or HTTPS authority is missing.</exception>
    internal static void Configure(
        ChronicleOptions options,
        X509Certificate2? certificate,
        ILogger<Kernel> logger,
        Action<int, HttpProtocols, X509Certificate2?> listen)
    {
        if (options.Tls.Enabled && certificate is null)
        {
            throw new InvalidOperationException(
                "No TLS certificate is configured. The Chronicle port serves gRPC (HTTP/2) and the Workbench, " +
                "API and OAuth flows (HTTP/1.1) on a single TLS port, which requires a certificate. " +
                "Provide one through Tls:CertificatePath (and Tls:CertificatePassword) in configuration. " +
                "When TLS is terminated upstream by an ingress/reverse proxy, re-encrypt the connection to Chronicle.");
        }

        if (!options.Tls.Enabled && options.Authentication.Enabled &&
            (!Uri.TryCreate(options.Authentication.Authority, UriKind.Absolute, out var authority) || authority.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "Authentication with Tls:Enabled=false requires an explicit HTTPS Authentication:Authority. " +
                "The internal OAuth authority cannot infer a safe public issuer from the cleartext backend. " +
                "Provide an external HTTPS authority, or keep TLS enabled on Chronicle.");
        }

        if (options.DedicatedHealthPort is not null && options.Health.Tls && certificate is null)
        {
            throw new InvalidOperationException(
                "The dedicated health port has Health:Tls=true but no TLS certificate is configured. " +
                "Configure Tls:CertificatePath or explicitly set Health:Tls=false for a private probe port.");
        }

        if (!options.Tls.Enabled)
        {
            logger.TlsDisabledBehindProxy();
        }

        listen(options.Port, options.Tls.Enabled ? HttpProtocols.Http1AndHttp2 : HttpProtocols.Http2, options.Tls.Enabled ? certificate : null);

        if (options.DedicatedHealthPort is { } healthPort)
        {
            logger.HealthEndpointListening(healthPort, options.Health.Tls);
            listen(healthPort, HttpProtocols.Http1, options.Health.Tls ? certificate : null);
        }
    }
}
