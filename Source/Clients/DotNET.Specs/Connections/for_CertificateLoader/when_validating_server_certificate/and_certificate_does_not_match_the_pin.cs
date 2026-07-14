// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate;

public class and_certificate_does_not_match_the_pin : Specification
{
    X509Certificate2 _certificate;
    bool _result;

    void Establish()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("cn=chronicle-test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        _certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: "0000000000000000000000000000000000000000")
        .Invoke(this, _certificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();

    void Destroy() => _certificate.Dispose();
}
