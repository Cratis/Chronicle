// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_validation_is_skipped : given.a_private_certificate_authority
{
    X509Certificate2 _unknownCertificateAuthority;
    X509Certificate2 _unknownServerCertificate;
    bool _result;

    void Establish()
    {
        _unknownCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-unknown-ca");
        _unknownServerCertificate = CreateServerCertificate(_unknownCertificateAuthority, "another.test");
    }

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: true, pinnedCertificateHash: null, _certificateAuthorities)
        .Invoke(this, _unknownServerCertificate, null, SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_accept_the_certificate() => _result.ShouldBeTrue();

    void Destroy()
    {
        _unknownServerCertificate.Dispose();
        _unknownCertificateAuthority.Dispose();
    }
}
