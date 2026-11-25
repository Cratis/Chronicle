// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Server.for_ClusterPeerCertificateValidator.given;

public class an_authority_and_certificates : Specification
{
    protected X509Certificate2 _authority;
    protected X509Certificate2 _local;
    protected X509Certificate2 _issuedByAuthority;
    protected X509Certificate2 _unrelated;

    void Establish()
    {
        using var authorityKey = RSA.Create(2048);
        var authorityRequest = new CertificateRequest("CN=chronicle-authority", authorityKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        authorityRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        authorityRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        using var authority = authorityRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        _authority = X509CertificateLoader.LoadCertificate(authority.RawData);

        _local = Issue(authority, "CN=local");
        _issuedByAuthority = Issue(authority, "CN=peer");
        _unrelated = SelfSigned("CN=unrelated");
    }

    void Destroy()
    {
        _authority.Dispose();
        _local.Dispose();
        _issuedByAuthority.Dispose();
        _unrelated.Dispose();
    }

    static X509Certificate2 Issue(X509Certificate2 authority, string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var serial = RandomNumberGenerator.GetBytes(8);
        using var issued = request.Create(authority, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(10), serial);
        return issued.CopyWithPrivateKey(key);
    }

    static X509Certificate2 SelfSigned(string subject)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadCertificate(created.RawData);
    }
}
