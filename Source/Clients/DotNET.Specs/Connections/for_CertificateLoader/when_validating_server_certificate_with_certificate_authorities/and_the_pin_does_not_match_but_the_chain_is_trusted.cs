// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_the_pin_does_not_match_but_the_chain_is_trusted : given.a_private_certificate_authority
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: "different-pin", _certificateAuthorities)
        .Invoke(this, _serverCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_accept_the_certificate() => _result.ShouldBeTrue();
}
