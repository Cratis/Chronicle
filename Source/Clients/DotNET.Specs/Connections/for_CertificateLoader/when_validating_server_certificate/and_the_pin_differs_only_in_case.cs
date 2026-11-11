// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;

namespace Cratis.Chronicle.Connections.for_CertificateLoader.when_validating_server_certificate;

public class and_the_pin_differs_only_in_case : given.a_private_certificate_authority
{
    bool _result;

    void Because() => _result = CertificateLoader
        .CreateServerCertificateValidationCallback(skipTlsValidation: false, pinnedCertificateHash: _serverCertificate.GetCertHashString().ToLowerInvariant())
        .Invoke(this, _serverCertificate, null, SslPolicyErrors.RemoteCertificateChainErrors);

    [Fact] void should_reject_the_certificate() => _result.ShouldBeFalse();
}
