// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Orleans.Connections.Security;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Encrypts and authenticates the Orleans silo and gateway traffic with the certificate Chronicle already serves, when there is one.
/// </summary>
internal static class ClusterTransportSecurity
{
    static readonly object _authoritiesLock = new();
    static string? _authoritiesFor;
    static X509Certificate2Collection _authorities = [];

    /// <summary>
    /// Determines whether the cluster transport is secured for the configuration.
    /// </summary>
    /// <param name="clustering">The clustering configuration.</param>
    /// <param name="certificate">The reloading certificate loaded from a file, or null when there is none.</param>
    /// <returns>True when the transport is secured; otherwise false.</returns>
    internal static bool IsSecured(Clustering clustering, ReloadingCertificate? certificate) =>
        certificate is not null && clustering.TlsEnabled && clustering.Type is ClusteringType.MongoDB or ClusteringType.Sql;

    /// <summary>
    /// Secures the cluster transport when the configuration and certificate allow it, and leaves it as it was otherwise.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="clustering">The clustering configuration.</param>
    /// <param name="tls">The TLS configuration the certificate came from.</param>
    /// <param name="certificate">The reloading certificate loaded from a file, or null when there is none.</param>
    /// <param name="logger">The kernel logger.</param>
    internal static void Configure(ISiloBuilder silo, Clustering clustering, Tls tls, ReloadingCertificate? certificate, ILogger<Kernel> logger)
    {
        if (clustering.Type is not (ClusteringType.MongoDB or ClusteringType.Sql))
        {
            return;
        }

        if (!clustering.TlsEnabled)
        {
            logger.ClusterTrafficTlsDisabled();
            return;
        }

        if (certificate is null)
        {
            logger.ClusterTrafficNotEncrypted();
            return;
        }

        silo.UseTls(options => Apply(options, tls, certificate));
        logger.ClusterTrafficEncrypted();
    }

    /// <summary>
    /// Applies the certificate and the peer authentication to the TLS options.
    /// </summary>
    /// <param name="options">The Orleans TLS options.</param>
    /// <param name="tls">The TLS configuration the certificate came from.</param>
    /// <param name="certificate">The certificate to present.</param>
    internal static void Apply(TlsOptions options, Tls tls, ReloadingCertificate certificate)
    {
        options.LocalCertificate = certificate.Current;
        options.LocalServerCertificateSelector = (_, _) => certificate.Current;
        options.LocalClientCertificateSelector = (_, _, _, _, _) => certificate.Current;
        options.RemoteCertificateMode = RemoteCertificateMode.RequireCertificate;
        options.ClientCertificateMode = RemoteCertificateMode.RequireCertificate;
        options.RemoteCertificateValidation = (peer, chain, _) => ClusterPeerCertificateValidator.IsTrusted(certificate.Current, peer, AuthoritiesFor(tls, certificate.Current), chain);
    }

    /// <summary>
    /// Gets the other certificates stored next to the presented certificate, such as the issuing authority a cert-manager keystore carries.
    /// </summary>
    /// <param name="tls">The TLS configuration.</param>
    /// <param name="current">The certificate currently presented.</param>
    /// <returns>The certificates found, empty when the file holds only the certificate itself or cannot be read.</returns>
    static X509Certificate2Collection AuthoritiesFor(Tls tls, X509Certificate2 current)
    {
        lock (_authoritiesLock)
        {
            if (_authoritiesFor == current.Thumbprint)
            {
                return _authorities;
            }

            var authorities = new X509Certificate2Collection();
            try
            {
                if (!string.IsNullOrEmpty(tls.CertificatePath) && File.Exists(tls.CertificatePath))
                {
                    foreach (var candidate in X509CertificateLoader.LoadPkcs12CollectionFromFile(tls.CertificatePath, tls.CertificatePassword))
                    {
                        if (candidate.Thumbprint != current.Thumbprint)
                        {
                            authorities.Add(candidate);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or System.Security.Cryptography.CryptographicException or UnauthorizedAccessException)
            {
                authorities = [];
            }

            _authorities = authorities;
            _authoritiesFor = current.Thumbprint;
            return authorities;
        }
    }
}
