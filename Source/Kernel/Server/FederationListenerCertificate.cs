// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Federation;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Holds the certificate the federation listener presents when hosted federation issues it from the kernel's own authority,
/// and renews it once it is past half of its lifetime. The private key lives only in memory.
/// </summary>
/// <param name="keys">The <see cref="IFederationAuthorityKeys"/> holding the authority.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
internal sealed class FederationListenerCertificate(IFederationAuthorityKeys keys, IOptions<ChronicleOptions> options, TimeProvider timeProvider)
{
    volatile SslStreamCertificateContext? _context;
    DateTimeOffset _renewAfter;

    /// <summary>
    /// Gets the context to present, or null until the first certificate has been issued, in which case handshakes are refused.
    /// </summary>
    public SslStreamCertificateContext? Current => _context;

    /// <summary>
    /// Issue a listener certificate when there is none or the current one is past half of its lifetime.
    /// </summary>
    /// <returns>True when a certificate was issued.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the public endpoint is not configured.</exception>
    public async Task<bool> Refresh()
    {
        var now = timeProvider.GetUtcNow();
        if (_context is not null && now < _renewAfter)
        {
            return false;
        }

        var hosted = options.Value.Federation.Hosted;
        if (!Uri.TryCreate(hosted.PublicEndpoint, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException("Federation:Hosted:PublicEndpoint must be an absolute https URI so the kernel can issue its listener certificate. It has no default.");
        }

        var (authority, _) = await keys.GetOrCreate();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var csr = new CertificateRequest("CN=chronicle-federation-listener", key, HashAlgorithmName.SHA256).CreateSigningRequest();
        using var issued = FederationCertificateAuthority.IssueServer(authority, csr, endpoint.DnsSafeHost, now.AddMinutes(-5), now.Add(hosted.ListenerCertificateLifetime));

        // The certificate with its key is re-created from PKCS#12 so the key is usable by the TLS stack on every platform.
        var withKey = issued.CopyWithPrivateKey(key);
        var exported = X509CertificateLoader.LoadPkcs12(withKey.Export(X509ContentType.Pfx), null);
        using var authorityPublic = X509CertificateLoader.LoadCertificate(authority.RawData);
        _context = SslStreamCertificateContext.Create(exported, [authorityPublic], offline: true);
        _renewAfter = now + (hosted.ListenerCertificateLifetime / 2);
        return true;
    }
}
