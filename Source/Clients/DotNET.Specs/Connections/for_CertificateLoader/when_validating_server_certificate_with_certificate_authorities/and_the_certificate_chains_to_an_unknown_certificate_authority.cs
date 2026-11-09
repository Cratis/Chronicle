// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_the_certificate_chains_to_an_unknown_certificate_authority : given.a_private_certificate_authority
{
    X509Certificate2 _unknownCertificateAuthority;
    X509Certificate2 _unknownServerCertificate;
    bool _result;

    void Establish()
    {
        _unknownCertificateAuthority = CreateCertificateAuthority("CN=chronicle-specs-unknown-ca");
        _unknownServerCertificate = CreateServerCertificate(_unknownCertificateAuthority, ServerName);
    }

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: null, _certificateAuthorities)
        .Invoke(this, _unknownServerCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();

    void Destroy()
    {
        _unknownServerCertificate.Dispose();
        _unknownCertificateAuthority.Dispose();
    }
}
