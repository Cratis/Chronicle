// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Federation.given;

public class certificates : Specification
{
    protected X509Certificate2 _certificate;
    protected X509Certificate2 _otherCertificate;

    void Establish()
    {
        _certificate = Create("peer-a");
        _otherCertificate = Create("peer-b");
    }

    void Destroy()
    {
        _certificate.Dispose();
        _otherCertificate.Dispose();
    }

    protected static X509Certificate2 Create(string name)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={name}", key, HashAlgorithmName.SHA256);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }
}
