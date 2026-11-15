// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.given;

public class a_private_certificate_authority : Specification
{
    protected const string ServerName = "chronicle.test";

    protected string _certificateAuthorityPath;
    protected X509Certificate2 _certificateAuthority;
    protected X509Certificate2 _serverCertificate;
    protected X509Certificate2Collection _certificateAuthorities;

    void Establish()
    {
        _certificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-ca");
        _serverCertificate = CreateServerCertificate(_certificateAuthority, ServerName);
        _certificateAuthorityPath = Path.Combine(Path.GetTempPath(), $"chronicle-client-ca-{Guid.NewGuid():N}.pem");
        File.WriteAllText(_certificateAuthorityPath, _certificateAuthority.ExportCertificatePem());
        _certificateAuthorities = CertificateLoader.LoadCertificateAuthorities(_certificateAuthorityPath);
    }

    void Destroy()
    {
        foreach (var certificate in _certificateAuthorities ?? [])
        {
            certificate.Dispose();
        }

        _serverCertificate?.Dispose();
        _certificateAuthority?.Dispose();

        if (File.Exists(_certificateAuthorityPath))
        {
            File.Delete(_certificateAuthorityPath);
        }
    }

    protected static X509Certificate2 CreateCertificateAuthority(string subject) =>
        CreateCertificateAuthority(subject, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));

    protected static X509Certificate2 CreateCertificateAuthority(string subject, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    protected static X509Certificate2 CreateServerCertificate(X509Certificate2 issuer, string serverName) =>
        CreateServerCertificate(issuer, serverName, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(7));

    protected static X509Certificate2 CreateServerCertificate(X509Certificate2 issuer, string serverName, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest($"CN={serverName}", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));
        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName(serverName);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(issuer, includeKeyIdentifier: true, includeIssuerAndSerial: false));
        return request.Create(issuer, notBefore, notAfter, [0x01, 0x23, 0x45, 0x67, 0x89]);
    }
}
