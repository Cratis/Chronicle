// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate_with_certificate_authorities;

public class and_there_is_no_certificate : given.a_private_certificate_authority
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: null, _certificateAuthorities)
        .Invoke(this, null, null, SslPolicyErrors.None);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();
}
