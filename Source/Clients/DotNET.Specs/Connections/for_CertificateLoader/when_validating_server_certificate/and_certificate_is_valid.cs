// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate;

public class and_certificate_is_valid : Specification
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: null)
        .Invoke(this, null, null, SslPolicyErrors.None);

    [Fact] void should_accept_the_certificate() => _result.ShouldBeTrue();
}
