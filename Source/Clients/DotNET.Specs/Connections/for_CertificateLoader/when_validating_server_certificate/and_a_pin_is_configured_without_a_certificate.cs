// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate;

public class and_a_pin_is_configured_without_a_certificate : Specification
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: "configured-pin")
        .Invoke(this, null, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();
}
