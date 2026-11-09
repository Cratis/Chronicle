// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_the_certificate_matches_the_pin_but_the_name_does_not_match : given.a_private_certificate_authority
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: _serverCertificate.GetCertHashString(), _certificateAuthorities)
        .Invoke(this, _serverCertificate, null, SslPolicyErrors.RemoteCertificateNameMismatch);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();
}
