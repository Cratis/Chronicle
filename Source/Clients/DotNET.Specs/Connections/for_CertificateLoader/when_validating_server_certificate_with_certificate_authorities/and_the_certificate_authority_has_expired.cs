// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_the_certificate_authority_has_expired : given.a_private_certificate_authority
{
    X509Certificate2 _expiredCertificateAuthority;
    X509Certificate2 _expiredServerCertificate;
    bool _result;

    void Establish()
    {
        _expiredCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-expired-ca", DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(-1));
        _expiredServerCertificate = CreateServerCertificate(_expiredCertificateAuthority, ServerName, DateTimeOffset.UtcNow.AddDays(-29), DateTimeOffset.UtcNow.AddDays(-2));
    }

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: null, [_expiredCertificateAuthority])
        .Invoke(this, _expiredServerCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();

    void Destroy()
    {
        _expiredServerCertificate.Dispose();
        _expiredCertificateAuthority.Dispose();
    }
}
