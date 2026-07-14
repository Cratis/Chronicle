// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate;

public class and_validation_is_skipped : Specification
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: true, pinnedCertificateHash: null)
        .Invoke(this, null, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_accept_the_invalid_certificate() => _result.ShouldBeTrue();
}
