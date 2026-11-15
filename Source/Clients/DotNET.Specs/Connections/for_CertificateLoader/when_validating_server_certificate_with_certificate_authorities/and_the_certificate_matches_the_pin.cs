// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_the_certificate_matches_the_pin : given.a_private_certificate_authority
{
    X509Certificate2 _pinnedCertificateAuthority;
    X509Certificate2 _pinnedServerCertificate;
    bool _result;

    void Establish()
    {
        _pinnedCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-pinned-ca");
        _pinnedServerCertificate = CreateServerCertificate(_pinnedCertificateAuthority, ServerName);
    }

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: _pinnedServerCertificate.GetCertHashString(), _certificateAuthorities)
        .Invoke(this, _pinnedServerCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_accept_the_certificate() => _result.ShouldBeTrue();

    void Destroy()
    {
        _pinnedServerCertificate.Dispose();
        _pinnedCertificateAuthority.Dispose();
    }
}
