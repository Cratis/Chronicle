// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Storage.AzureKeyVault.for_EmulatorCertificateValidation.given;

public class certificates : Specification
{
    protected X509Certificate2 _trustedRoot;
    protected X509Certificate2 _unrelated;
    protected X509Certificate2 _expired;
    protected bool _isTrusted;
    protected X509Chain _chain;

    void Establish()
    {
        _trustedRoot = CreateCertificate(DateTimeOffset.UtcNow.AddDays(1));
        _unrelated = CreateCertificate(DateTimeOffset.UtcNow.AddDays(1));
        _expired = CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1));
        _chain = new X509Chain { ChainPolicy = EmulatorCertificateValidation.CreatePolicy(_trustedRoot) };
    }

    void Destroy()
    {
        _chain.Dispose();
        _trustedRoot.Dispose();
        _unrelated.Dispose();
        _expired.Dispose();
    }

    static X509Certificate2 CreateCertificate(DateTimeOffset notAfter)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), notAfter);
    }
}
