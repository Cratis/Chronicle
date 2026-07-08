// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server;

/// <summary>
/// Provides an in-memory self-signed certificate for development use.
/// </summary>
/// <remarks>
/// The Chronicle port multiplexes gRPC (HTTP/2) and the Workbench, API and OAuth flows (HTTP/1.1)
/// on a single port, which requires TLS so that ALPN can negotiate the protocol per connection.
/// In development, when no certificate is configured, a self-signed certificate is generated so the
/// server works out of the box without any certificate setup.
/// </remarks>
public static class DevelopmentCertificate
{
    /// <summary>
    /// Creates a self-signed certificate valid for <c>localhost</c> and the loopback addresses.
    /// </summary>
    /// <returns>A self-signed <see cref="X509Certificate2"/> with a private key.</returns>
    public static X509Certificate2 Create()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName("localhost");
        subjectAlternativeNames.AddDnsName("chronicle");
        subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
        subjectAlternativeNames.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                critical: true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1")],
                critical: true));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(1));

        // Round-trip through PFX so the returned certificate owns an exportable, ephemeral private key
        // that Kestrel can use across the process lifetime.
        return X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pfx),
            password: null);
    }
}
